using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterSectionPositionsCoordinatesHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    ILogger<RegisterSectionPositionsCoordinatesHandler> logger)
    : BaseValidatorHandler<RegisterSectionPositionsCoordinatesCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(RegisterSectionPositionsCoordinatesCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType != RoleType.Administrator)
            return _errorManager.ThrowUnauthorized<Unit>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");

        var warehouse = await _unitOfWork.Warehouses.Entities
            .FirstOrDefaultAsync(w => w.IsActive && w.Id == request.WarehouseId, cancellationToken);

        if (warehouse is null)
            return _errorManager.ThrowBadRequest<Unit>("La bodega a la que intentas realizar el registro no existe", "ERP:NOT_FOUND");

        var section = await _unitOfWork.Sections.Entities
            .FirstOrDefaultAsync(s => s.IsActive && s.Id == request.SectionId && s.WarehouseId == warehouse.Id, cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<Unit>("La seccion seleccionada no pertenece o existe para esta bodega", "ERP:NOT_FOUND");

        foreach (var info in request.PositionsInformation)
        {
            var position = await _unitOfWork.SectionPositionsRepository.Entities
                .FirstOrDefaultAsync(p => p.Id == info.SectionPositionId && p.SectionId == section.Id, cancellationToken);

            if (position is null)
            {
                logger.LogWarning("Posición de pasillo {SectionPositionId} no encontrada, omitiendo.", info.SectionPositionId);
                continue;
            }

            var existing = await _unitOfWork.SectionPositionsCoordinates.Entities
                .FirstOrDefaultAsync(c => c.SectionPositionId == position.Id, cancellationToken);

            if (existing is not null)
            {
                existing.PositionX = info.PositionX;
                existing.PositionY = info.PositionY;
                existing.PositionZ = info.PositionZ;
                existing.RotationY = info.RotationY;

                await _unitOfWork.SectionPositionsCoordinates.UpdateAsync(existing);
            }
            else
            {
                var coordinate = new SectionPositionsCoordinates
                {
                    SectionPositionId = position.Id,
                    PositionX = info.PositionX,
                    PositionY = info.PositionY,
                    PositionZ = info.PositionZ,
                    RotationY = info.RotationY
                };

                await _unitOfWork.SectionPositionsCoordinates.RegisterSectionPositionCoordinate(coordinate);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
