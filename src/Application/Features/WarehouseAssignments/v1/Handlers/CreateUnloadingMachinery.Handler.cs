using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers
{
    public class CreateUnloadingMachineryHandler : BaseValidatorHandler<CreateUnloadingMachineryCommand, bool>
    {
        public CreateUnloadingMachineryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<bool> Handle(CreateUnloadingMachineryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var assignment = await WarehouseAssignmentRules.GetActiveAssignmentAsync(
                _unitOfWork, request.ReceptionId, request.EntranceDucatId, cancellationToken);

            if (assignment == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe asignar primero una bodega antes de asignar maquinaria.",
                    "ERP:ASSIGNMENT_NOT_FOUND");
            }

            Guid? internalMachineryId = null;

            if (!request.IsOutsourced)
            {
                var resolveResult = await ResolveInternalMachineryAndOperatorAsync(request, cancellationToken);
                if (!resolveResult.IsSuccess) return false;
                internalMachineryId = resolveResult.MachineryId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.ProviderName))
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "Debe ingresar el nombre del proveedor para la maquinaria tercerizada.",
                        "ERP:PROVIDER_NAME_REQUIRED");
                }
            }

            var machineryAssignment = new MachineryAssignments
            {
                Id = Guid.NewGuid(),
                WarehouseAssignmentId = assignment.Id,
                MachineryCode = internalMachineryId,
                OperatorCollaboratorId = request.IsOutsourced ? null : request.OperatorCollaboratorId,
                IsOutsourced = request.IsOutsourced,
                StartTime = request.StartTime != default ? request.StartTime : NicaraguaClock.Now,
                ProviderName = request.IsOutsourced ? request.ProviderName?.Trim() : null,
                InvoiceNumber = request.IsOutsourced ? request.InvoiceNumber?.Trim() : null,
                MachineryDescription = request.IsOutsourced ? request.MachineryDescription?.Trim() : null,
                AssignedByUserId = request.UserId.ToString()
            };

            await _unitOfWork.MachineryAssignments.InsertMachineryAssignment(machineryAssignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }

        private async Task<(bool IsSuccess, Guid? MachineryId)> ResolveInternalMachineryAndOperatorAsync(
            CreateUnloadingMachineryCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.MachineryCode))
            {
                _errorManager.ThrowBadRequest<bool>(
                    "Debe especificar la maquinaria interna a asignar.",
                    "ERP:MACHINERY_REQUIRED");
                return (false, null);
            }

            Guid? parsedId = null;

            if (Guid.TryParse(request.MachineryCode, out Guid mcGuid))
            {
                parsedId = mcGuid;
                var exists = await _unitOfWork.WarehouseMachineries.Entities
                    .AnyAsync(m => m.Id == mcGuid 
                                && m.CompanyId == request.CompanyId 
                                && m.IsActive 
                                && m.DeletedAt == null, cancellationToken);

                if (!exists)
                {
                    _errorManager.ThrowBadRequest<bool>(
                        "La maquinaria seleccionada no existe o no se encuentra activa.",
                        "ERP:MACHINERY_NOT_FOUND");
                    return (false, null);
                }
            }
            else
            {
                var machinery = await _unitOfWork.WarehouseMachineries.Entities
                    .FirstOrDefaultAsync(m => m.Code == request.MachineryCode.Trim() 
                                           && m.CompanyId == request.CompanyId 
                                           && m.IsActive 
                                           && m.DeletedAt == null, cancellationToken);

                if (machinery == null)
                {
                    _errorManager.ThrowBadRequest<bool>(
                        "La maquinaria con el codigo especificado no existe o no se encuentra activa.",
                        "ERP:MACHINERY_NOT_FOUND");
                    return (false, null);
                }

                parsedId = machinery.Id;
            }

            if (request.OperatorCollaboratorId.HasValue)
            {
                var operatorExists = await _unitOfWork.Collaborators.Entities
                    .AnyAsync(c => c.Id == request.OperatorCollaboratorId.Value 
                                && c.CompanyId == request.CompanyId 
                                && c.DeletedAt == null 
                                && c.Status == CollaboratorStatus.Active, cancellationToken);

                if (!operatorExists)
                {
                    _errorManager.ThrowBadRequest<bool>(
                        "El operador seleccionado no existe o no se encuentra activo.",
                        "ERP:OPERATOR_NOT_FOUND");
                    return (false, null);
                }
            }

            return (true, parsedId);
        }
    }
}
