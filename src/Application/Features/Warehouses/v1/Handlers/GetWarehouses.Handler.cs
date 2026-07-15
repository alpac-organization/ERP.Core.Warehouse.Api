using MediatR;
using AutoMapper;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class GetWarehousesHandler(IUnitOfWork _unitOfWork, IMapper _mapper) : IRequestHandler<GetWarehousesQuery, List<WarehouseDto>>
    {
        public async Task<List<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
        {
            var warehouses = await _unitOfWork.Warehouses.Entities
                .Where(ware => ware.IsActive)
                .ToListAsync(cancellationToken);
                
            return _mapper.Map<List<WarehouseDto>>(warehouses);
        }
    }
}