using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetWarehousesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetWarehousesQuery, List<WarehouseDto>>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var baseQuery = _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Where(ware => ware.IsActive && ware.ParentWarehouseId == null);

        if (!string.IsNullOrEmpty(request.BranchCode))
        {
            baseQuery = baseQuery.Where(ware => ware.Branch.BranchCode == request.BranchCode);
        }

        var warehouses = await baseQuery
            .Include(ware => ware.Branch)
            .Include(ware => ware.SubWarehouses.Where(sub => sub.IsActive))
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<WarehouseDto>>(warehouses);
    }
}