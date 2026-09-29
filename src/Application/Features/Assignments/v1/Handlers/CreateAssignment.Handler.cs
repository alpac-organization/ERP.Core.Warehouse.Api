using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class CreateAssignmentHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<CreateAssignmentCommand, Unit>(_unitOfWork, _errorManager)
    {
        public override async Task<Unit> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var operationalOrder = await _unitOfWork.OperationalOrders.Entities
                .Where(operation => operation.CompanyId == request.CompanyId)
                .Where(operation => operation.Status == OperationalOrderStatus.Assignment)
                .FirstOrDefaultAsync(cancellationToken);

            if (operationalOrder == null)
            {
                return _errorManager.ThrowNotFound<Unit>("La orden operativa no existe, o no esta en proceso de asignamiento", "ERP:NOT_FOUND_OPERATIONAL_ORDER");
            }

            var assignmentOperationalEntity = AssignmentMapper.ToAssignmentOperationalEntity(request);


            await _unitOfWork.AssignmentOperationals.RegisterAssignmentOperational(assignmentOperationalEntity);

            if (request.HasAssignedCollaborators)
            {
                assignmentOperationalEntity.HasCollaboratorsAssigned = true;

                //Insert de colaboradores y destipulaciones de roles

                foreach (var collaborator in request.AssignedCollaborators)
                {
                    var assignmentCollaborator = AssignmentMapper.ToAssignmentCollaboratorsEntity(collaborator, assignmentOperationalEntity.Id);
                    assignmentCollaborator.CreatedByUserId = access.User.Id;

                    await _unitOfWork.AssignmentCollaborators.AssignCollaborator(assignmentCollaborator);
                }
            }

            if (request.HasAssignedMachinery)
            {
                assignmentOperationalEntity.HasMachineryAssigned = true;

                foreach (var machinery in request.AssignedMachineries)
                {
                    var assignmentMachinery = AssignmentMapper.ToAssignmentsMachineryEntity(machinery, assignmentOperationalEntity.Id);
                    assignmentMachinery.CreatedByUserId = access.User.Id;

                    await _unitOfWork.AssignmentsMachineries.AssignMachinery(assignmentMachinery);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}