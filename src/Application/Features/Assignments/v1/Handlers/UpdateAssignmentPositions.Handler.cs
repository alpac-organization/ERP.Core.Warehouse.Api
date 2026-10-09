using MediatR;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class UpdateAssignmentPositionsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
        : BaseValidatorHandler<UpdateAssignmentPositionsCommand, Unit>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions SnakeCaseOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public override async Task<Unit> Handle(UpdateAssignmentPositionsCommand request, CancellationToken cancellationToken)
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

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Where(a => a.Id == request.AssignmentOperationalId)
                .Where(a => a.OperationalOrderId == request.OperationalOrderId)
                .Where(a => a.IsActive && a.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);

            if (assignment is null)
            {
                return _errorManager.ThrowNotFound<Unit>("No se encontró la asignación solicitada.", "ERP:ASSIGNMENT_NOT_FOUND");
            }

            if (assignment.Status != AssignmentOperationalStatus.InProgress)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "Debe asignar las posiciones antes de registrar la información de polines.", "ERP:ASSIGNMENT_POST_REQUIRED");
            }

            var pallets = request.Pallets
                .Select(p => new PositionatingInformation
                {
                    Type = p.Type,
                    CountPallets = p.CountPallets,
                    Width = p.Type == PalletType.Standard ? 1 : p.Width,
                    Length = p.Type == PalletType.Standard ? 1.2m : p.Length,
                    BulksPerPallet = request.MerchandiseType == UnloadingMerchandiseType.Bulk ? p.BulksPerPallet : null
                })
                .ToList();

            assignment.MerchandiseType = request.MerchandiseType;
            assignment.HasPositionatingInformation = true;
            assignment.AdditionalData = JsonSerializer.Serialize(
                new AdditionalDataAssingmentOperational { PositionatingInformation = pallets },
                SnakeCaseOptions);

            await _unitOfWork.AssignmentOperationals.UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}