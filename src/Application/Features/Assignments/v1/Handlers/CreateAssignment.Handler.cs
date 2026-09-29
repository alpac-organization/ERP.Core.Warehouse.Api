using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class CreateAssignmentHandler(
        IUnitOfWork _unitOfWork, 
        IErrorManager _errorManager, 
        ICodeGenerator _codeGenerator,
        IMapper _mapper) : BaseValidatorHandler<CreateAssignmentCommand, Unit>(_unitOfWork, _errorManager)
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
                .FirstOrDefaultAsync(oo => oo.Id == request.OperationalOrderId, cancellationToken);

            if (operationalOrder == null)
            {
                return _errorManager.ThrowNotFound<Unit>("La orden operativa no existe", "ERP:NOT_FOUND_OPERATIONAL_ORDER");
            }

            var assignment = new AssignmentOperational
            {
                Id = Guid.NewGuid(),
                OperationalOrderId = request.OperationalOrderId,
                Status = request.Status,
                HasMachineryAssigned = request.Machineries.Count > 0,
                HasEnclosureAssigned = request.Enclosure != null,
                HasCollaboratorsAssigned = request.Collaborators.Count > 0,
                AdditionalData = "{}"
            };

            await _unitOfWork.AssignmentOperationals.AddAsync(assignment);

            if (request.Enclosure != null)
            {
                var enclosure = new AssignmentEnclosure
                {
                    Id = Guid.NewGuid(),
                    AssignmentOperationalId = assignment.Id,
                    Observations = request.Enclosure.Observations,
                    Merchandise = request.Enclosure.Merchandise,
                    MerchandiseDescription = request.Enclosure.MerchandiseDescription,
                    DestinationType = request.Enclosure.DestinationType,
                    WarehouseId = request.Enclosure.WarehouseId
                };
                await _unitOfWork.AssignmentEnclosures.AddAsync(enclosure);
            }

            foreach (var machinery in request.Machineries)
            {
                var machineryAssignment = new AssignmentsMachinery
                {
                    Id = Guid.NewGuid(),
                    AssignmentOperationalId = assignment.Id,
                    MachineryId = machinery.MachineryId,
                    Concept = machinery.Concept,
                    IsActive = true,
                    CreatedByUserId = access.User.Id
                };
                await _unitOfWork.AssignmentsMachineries.AddAsync(machineryAssignment);
            }

            foreach (var collaborator in request.Collaborators)
            {
                var collaboratorAssignment = new AssignmentCollaborators
                {
                    Id = Guid.NewGuid(),
                    AssignmentOperationalId = assignment.Id,
                    CollaboratorId = collaborator.CollaboratorId,
                    Role = collaborator.Role,
                    IsActive = true,
                    CreatedByUserId = access.User.Id,
                    OperationalOrderId = request.OperationalOrderId
                };
                await _unitOfWork.AssignmentCollaborators.AddAsync(collaboratorAssignment);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            operationalOrder.HasMachineryAssigned = request.Machineries.Count > 0;
            operationalOrder.HasEnclosureAssigned = request.Enclosure != null;
            operationalOrder.HasCollaboratorsAssigned = request.Collaborators.Count > 0;
            await _unitOfWork.OperationalOrders.UpdateAsync(operationalOrder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}