using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetWarehouseByIdHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    IWarehouseCapacityCalculator capacityCalculator)
    : BaseValidatorHandler<GetWarehouseByIdQuery, WarehouseDetailDto>(unitOfWork, errorManager)
{
    public override async Task<WarehouseDetailDto> Handle(
        GetWarehouseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var warehouse = await _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Include(w => w.Capacity)
            .Include(w => w.Branch)
            .Include(w => w.Details)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Racks)
                    .ThenInclude(r => r.Positions)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Lots)
                    .ThenInclude(l => l.Positions)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Capacity)
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (warehouse == null)
            return _errorManager.ThrowNotFound<WarehouseDetailDto>(
                "La bodega no fue encontrada.",
                "ERP:WAREHOUSE_NOT_FOUND");

        return await WarehouseDetailBuilder.BuildDetailAsync(
            warehouse, mapper, capacityCalculator, cancellationToken);
    }
}