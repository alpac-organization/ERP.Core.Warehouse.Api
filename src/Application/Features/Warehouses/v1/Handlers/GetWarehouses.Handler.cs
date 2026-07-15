using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class GetWarehousesHandler(IUnitOfWork _unitOfWork, IMapper _mapper) : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
    {
        public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
        {
            var warehousesQuery = _unitOfWork.Warehouses.Entities
                .Where(ware => ware.IsActive)
                .Include(ware => ware.Branch)
                .AsNoTracking();


            if (!string.IsNullOrEmpty(request.BranchCode))
            {
                warehousesQuery = warehousesQuery
                    .Where(ware => ware.Branch.BranchCode == request.BranchCode);
            }

            var warehouses = await warehousesQuery
                .FirstOrDefaultAsync(cancellationToken);
                
            return _mapper.Map<List<WarehouseDto>>(warehouses);
        }
    }
}