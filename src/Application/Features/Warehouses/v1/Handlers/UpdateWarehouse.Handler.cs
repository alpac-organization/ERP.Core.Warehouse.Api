using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using WarehouseEntity = ERP.Core.Database.Domain.Entities.Warehouse.Warehouses;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class UpdateWarehouseHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    ILogger<UpdateWarehouseHandler> logger,
    IMapper mapper,
    IWarehouseCapacityCalculator calculator)
    : BaseValidatorHandler<UpdateWarehouseCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse;

        if (access.Role?.RoleType is RoleType.Operator or RoleType.Supervisor)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No tienes permiso para realizar esta acción", "ERP:01");
        }

        logger.LogInformation(
            "Iniciando actualización de almacén. WarehouseId={WarehouseId}",
            request.WarehouseId);

        var warehouse = await _unitOfWork.Warehouses.Entities
            .Include(w => w.WarehouseCapacity)
            .Include(w => w.WarehouseLocation)
            .FirstOrDefaultAsync(
                w => w.Id == request.WarehouseId && w.DeletedAt == null,
                cancellationToken);

        if (warehouse is null)
        {
            return _errorManager.ThrowNotFound<bool>(
                "El almacén indicado no existe.", "ERP:WAREHOUSE_NOT_FOUND");
        }

        if (request.Code is not null)
        {
            var code = request.Code.Trim();

            var codeExists = await _unitOfWork.Warehouses.Entities.AnyAsync(
                w => w.Code == code &&
                     w.DeletedAt == null &&
                     w.Id != request.WarehouseId,
                cancellationToken);

            if (codeExists)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Ya existe un almacén con ese código.", "ERP:01");
            }

            warehouse.Code = code;
        }

        if (request.IsActive.HasValue)
            warehouse.IsActive = request.IsActive.Value;

        if (request.WarehouseType.HasValue)
            warehouse.WarehouseType = request.WarehouseType.Value;

        if (request.Location is not null)
        {
            var locationUpdated = await UpdateWarehouseLocationAsync(
                request.Location, warehouse, cancellationToken);

            if (!locationUpdated)
                return false;
        }

        if (request.Capacity is not null)
        {
            var capacityUpdated = await UpdateWarehouseCapacityAsync(
                request.Capacity, warehouse, cancellationToken);

            if (!capacityUpdated)
                return false;
        }

        await _unitOfWork.Warehouses.UpdateAsync(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Almacén actualizado. WarehouseId={WarehouseId}",
            request.WarehouseId);

        return true;
    }

    private async Task<bool> UpdateWarehouseLocationAsync(
        UpdateWarehouseLocationDto request,
        WarehouseEntity warehouse,
        CancellationToken cancellationToken)
    {
        if (request.LocationName is null)
            return true;

        if (warehouse.WarehouseLocation is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El almacén no tiene ubicación registrada.",
                "ERP:WAREHOUSE_LOCATION_NOT_FOUND");
        }

        warehouse.WarehouseLocation.LocationName = request.LocationName.Trim();
        await _unitOfWork.Locations.UpdateAsync(warehouse.WarehouseLocation);
        return true;
    }

    private async Task<bool> UpdateWarehouseCapacityAsync(
        UpdateWarehouseCapacityDto request,
        WarehouseEntity warehouse,
        CancellationToken cancellationToken)
    {
        if (warehouse.WarehouseCapacity is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El almacén no tiene capacidad registrada.",
                "ERP:WAREHOUSE_CAPACITY_NOT_FOUND");
        }

        var stored = warehouse.WarehouseCapacity;

        var calculation = await calculator.UpdateWarehouseAsync(
            warehouse.Id,
            request.Width ?? stored.Width,
            request.Length ?? stored.Length,
            request.HasMargins ?? stored.HasMargins,
            request.MinimumHeight ?? stored.MinimumHeight,
            request.MaximumHeight ?? stored.MaximumHeight,
            request.MarginTop ?? stored.MarginTop,
            request.MarginBottom ?? stored.MarginBottom,
            request.MarginRight ?? stored.MarginRight,
            request.MarginLeft ?? stored.MarginLeft,
            cancellationToken);

        if (calculation.Warehouse is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se pudo recalcular la capacidad del almacén.", "ERP:01");
        }

        mapper.Map(calculation.Warehouse, stored);
        await _unitOfWork.WarehouseCapacities.UpdateAsync(stored);
        return true;
    }
}
