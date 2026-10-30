using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterSectionPositionsHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    ILogger<RegisterSectionPositionsHandler> logger)
    : BaseValidatorHandler<RegisterSectionPositionsCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(RegisterSectionPositionsCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType != RoleType.Administrator)
            return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");

        if (request.Rows <= 0 || request.Columns <= 0 || request.Levels <= 0)
            return _errorManager.ThrowBadRequest<bool>(
                "Las filas, columnas y niveles deben ser mayores a cero.",
                "ERP:SECTION_POSITION_DIMENSIONS_INVALID");

        var section = await _unitOfWork.Sections.Entities
            .Include(s => s.SectionCapacity)
            .FirstOrDefaultAsync(
                s => s.Id == request.SectionId && s.IsActive && s.DeletedAt == null,
                cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

        if (section.WarehouseId != request.WarehouseId)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no pertenece al almacén indicado.", "ERP:SECTION_WAREHOUSE_MISMATCH");

        if (section.SectionType != SectionType.Aisle || section.SectionStorageType != SectionStorageType.Pallets)
            return _errorManager.ThrowBadRequest<bool>(
                "Solo las secciones de tipo pasillo con almacenamiento en polines admiten posiciones.",
                "ERP:SECTION_TYPE_NOT_ALLOWED_FOR_POSITIONS");

        var sectionWidth = section.SectionCapacity?.Width ?? 0m;
        var sectionLength = section.SectionCapacity?.Length ?? 0m;

        if (sectionWidth <= 0 || sectionLength <= 0)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene dimensiones físicas válidas (ancho y largo) configuradas.",
                "ERP:SECTION_CAPACITY_NOT_CONFIGURED");

        var positionWidth = sectionWidth / request.Columns;
        var positionLength = sectionLength / request.Rows;

        for (int level = 1; level <= request.Levels; level++)
        {
            for (int row = 1; row <= request.Rows; row++)
            {
                for (int column = 1; column <= request.Columns; column++)
                {
                    var positionCode = $"{section.Code}-F{row}C{column}N{level}";

                    var position = new SectionPositions
                    {
                        Id = Guid.NewGuid(),
                        SectionId = section.Id,
                        PositionCode = positionCode,
                        Row = row,
                        Column = column,
                        Level = level,
                        Status = RackStatus.Available,
                        AllowsStocking = true,
                        Width = positionWidth,
                        Length = positionLength,
                        AreaM2 = positionWidth * positionLength
                    };

                    await _unitOfWork.SectionPositionsRepository.RegisterPosition(position);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Se registraron las posiciones del pasillo {SectionId} ({Rows}x{Columns}x{Levels}).",
            section.Id, request.Rows, request.Columns, request.Levels);

        return true;
    }
}
