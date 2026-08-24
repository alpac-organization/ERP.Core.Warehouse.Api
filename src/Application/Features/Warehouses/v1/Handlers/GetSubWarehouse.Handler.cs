using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetSubWarehousesHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    IWarehouseCapacityCalculator capacityCalculator)
    : BaseValidatorHandler<GetSubWarehousesQuery, PagedResponse<WarehouseDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<WarehouseDto>> Handle(
        GetSubWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var query = _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Where(w => w.ParentWarehouseId == request.ParentWarehouseId);

        if (request.IsActive.HasValue)
            query = query.Where(w => w.IsActive == request.IsActive.Value);

        if (request.IsOwner.HasValue)
            query = query.Where(w => w.IsOwner == request.IsOwner.Value);

        if (!string.IsNullOrWhiteSpace(request.WarehouseCode))
            query = query.Where(w => w.Code == request.WarehouseCode);

        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(w =>
                w.Code.Contains(request.Search) ||
                w.WarehouseName.Contains(request.Search));

        return await WarehousePagedQuery.ExecuteAsync(
            query.OrderBy(w => w.Code), request, mapper, capacityCalculator, cancellationToken);
    }
}