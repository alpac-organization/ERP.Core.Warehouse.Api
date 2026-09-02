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

            var assignmentQuery = _unitOfWork.WarehouseAssignments.Entities
                .Where(a => a.RecordEntranceId == request.ReceptionId && a.DeletedAt == null);

            if (request.EntranceDucatId.HasValue)
            {
                assignmentQuery = assignmentQuery.Where(a => a.EntranceDucatId == request.EntranceDucatId.Value);
            }
            else
            {
                assignmentQuery = assignmentQuery.Where(a => a.EntranceDucatId == null);
            }

            var assignment = await assignmentQuery.FirstOrDefaultAsync(cancellationToken);

            if (assignment == null)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe asignar primero una bodega antes de asignar maquinaria.",
                    "ERP:ASSIGNMENT_NOT_FOUND");
            }

            Guid? parsedMachineryCode = null;

            if (!request.IsOutsourced)
            {
                if (string.IsNullOrWhiteSpace(request.MachineryCode))
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "Debe especificar la maquinaria interna a asignar.",
                        "ERP:MACHINERY_REQUIRED");
                }

                if (Guid.TryParse(request.MachineryCode, out Guid mcGuid))
                {
                    parsedMachineryCode = mcGuid;
                    var machineryExists = await _unitOfWork.WarehouseMachineries.Entities
                        .AnyAsync(m => m.Id == mcGuid 
                                    && m.CompanyId == request.CompanyId 
                                    && m.IsActive 
                                    && m.DeletedAt == null, cancellationToken);

                    if (!machineryExists)
                    {
                        return _errorManager.ThrowBadRequest<bool>(
                            "La maquinaria seleccionada no existe o no se encuentra activa.",
                            "ERP:MACHINERY_NOT_FOUND");
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
                        return _errorManager.ThrowBadRequest<bool>(
                            "La maquinaria con el código especificado no existe o no se encuentra activa.",
                            "ERP:MACHINERY_NOT_FOUND");
                    }
                    parsedMachineryCode = machinery.Id;
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
                        return _errorManager.ThrowBadRequest<bool>(
                            "El operador seleccionado no existe o no se encuentra activo.",
                            "ERP:OPERATOR_NOT_FOUND");
                    }
                }
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
                MachineryCode = parsedMachineryCode,
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
    }
}
