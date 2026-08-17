using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class UpdateReceptionEntranceHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager)
    : BaseValidatorHandler<UpdateReceptionEntranceCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(UpdateReceptionEntranceCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance)
            .Include(r => r.EntranceDucats.Where(d => d.DeletedAt == null))
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(c => c.Details)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        #region 0. Restringir si el expediente ya avanzó de paso
        var hasAdvanced = await _unitOfWork.StepExecutionLogs.Entities
            .AnyAsync(l => l.RecordEntranceId == request.ReceptionId
                        && l.WorkflowStepDefinitionCode != WorkflowStepCodes.Reception,
                      cancellationToken);

        if (hasAdvanced)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No es posible editar este expediente porque ya avanzó a un paso posterior del proceso.",
                "ERP:RECORD_ALREADY_ADVANCED");
        }
        #endregion

        var documentType = recordEntrance.ReceptionEntrance?.DocumentType;

        #region 1. Validar que los campos enviados correspondan al tipo de documento
        bool sendsDucatFields = request.Ducats is not null;
        bool sendsCustomsFields = request.CustomsDeclarationNumber is not null
            || request.Packages is not null
            || request.Customer is not null
            || request.Product is not null;

        if (sendsDucatFields && documentType != DocumentType.DUCA)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden actualizar DUCA's en un expediente que no es de tipo DUCA.",
                "ERP:INVALID_FIELDS_FOR_DOCUMENT_TYPE");
        }

        if (sendsCustomsFields && documentType != DocumentType.CustomsDeclaration)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden actualizar campos de Declaración Aduanera en un expediente que no es de ese tipo.",
                "ERP:INVALID_FIELDS_FOR_DOCUMENT_TYPE");
        }
        #endregion

        #region 2. Sincronización de Ducas
        if (request.Ducats != null)
        {
            var requestedIds = request.Ducats.Select(d => d.Id).ToList();
            if (requestedIds.Any(id => !id.HasValue))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "No se permite crear nuevos DUCA's en esta operación.",
                    "ERP:DUCA_INSERT_NOT_ALLOWED");
            }

            var duplicatesInRequest = request.Ducats
                .GroupBy(d => d.DucatNumber, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatesInRequest.Count != 0)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    $"La lista de DUCA's contiene números duplicados en la misma solicitud: {string.Join(", ", duplicatesInRequest)}",
                    "ERP:REQUEST_DUPLICATED_DUCA");
            }

            var normalizedNumbersLower = request.Ducats.Select(d => d.DucatNumber.ToLower()).ToList();

            var duplicateGlobalDucas = await _unitOfWork.EntranceDucats.Entities
                .Where(d => d.RecordEntranceId != request.ReceptionId &&
                            d.DeletedAt == null &&
                            normalizedNumbersLower.Contains(d.DucatNumber.Trim().ToLower()))
                .Select(d => d.DucatNumber)
                .ToListAsync(cancellationToken);

            if (duplicateGlobalDucas.Count != 0)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    $"Los siguientes números DUCA ya están registrados en el sistema: {string.Join(", ", duplicateGlobalDucas)}.",
                    "ERP:GLOBAL_DUPLICATED_DUCA_ERROR");
            }

            var ducatIdsToUpdate = request.Ducats.Select(d => d.Id!.Value).ToList();

            var ducatsWithChildren = await _unitOfWork.EntranceDucats.Entities
                .Where(d => ducatIdsToUpdate.Contains(d.Id) && d.DeletedAt == null)
                .Where(d => d.Discrepancy != null || d.RegistryDetail != null)
                .Select(d => d.DucatNumber)
                .ToListAsync(cancellationToken);

            if (ducatsWithChildren.Count != 0)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    $"Las siguientes DUCA's tienen registros relacionados y no pueden editarse: {string.Join(", ", ducatsWithChildren)}",
                    "ERP:DUCA_HAS_RELATED_RECORDS");
            }

            foreach (var item in request.Ducats)
            {
                var ducat = recordEntrance.EntranceDucats.FirstOrDefault(d => d.Id == item.Id!.Value);

                if (ducat == null)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        $"La DUCA con id '{item.Id}' no pertenece a este registro de recepción.",
                        "ERP:DUCA_NOT_FOUND");
                }

                ducat.ApplyUpdate(item.DucatNumber);
            }
        }
        #endregion

        #region 3. Actualización de Declaración Aduanera
        if (sendsCustomsFields)
        {
            var declaration = recordEntrance.CustomsDeclarations;

            if (declaration == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Este expediente no tiene una Declaración Aduanera asociada.",
                    "ERP:CUSTOMS_DECLARATION_NOT_FOUND");
            }

            if (request.CustomsDeclarationNumber != null)
            {
                var duplicateNumberExists = await _unitOfWork.CustomsDeclarations.Entities
                    .AnyAsync(d => d.Id != declaration.Id
                                && d.DeletedAt == null
                                && d.CustomsDeclarationNumber.Trim().ToLower() == request.CustomsDeclarationNumber.ToLower(),
                              cancellationToken);

                if (duplicateNumberExists)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        $"El número de declaración aduanera {request.CustomsDeclarationNumber} ya está registrado en el sistema.",
                        "ERP:GLOBAL_DUPLICATED_CUSTOMS_DECLARATION");
                }
            }

            declaration.ApplyUpdate(request);
            declaration.Details?.ApplyUpdate(request);
        }
        #endregion

        #region 4. Actualización parcial de los campos de la recepción
        if (recordEntrance.ReceptionEntrance != null)
        {
            var user = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            var nowNica = NicaraguaClock.Now;

            recordEntrance.ReceptionEntrance.ApplyUpdate(
                request,
                request.UserId.ToString(),
                user?.Fullname ?? user?.UserName ?? request.UserId.ToString(),
                DateOnly.FromDateTime(nowNica),
                TimeOnly.FromDateTime(nowNica));
        }
        #endregion

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}