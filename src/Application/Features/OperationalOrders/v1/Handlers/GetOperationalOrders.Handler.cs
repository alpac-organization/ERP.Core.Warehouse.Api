using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Handlers
{
    public class GetOperationalOrdersHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetOperationalOrdersQuery, PagedResponse<OperationalOrderDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<OperationalOrderDto>> Handle(GetOperationalOrdersQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var operationalOrdersQuery = _unitOfWork.OperationalOrders.Entities
                .Include(po => po.Customer)
                .Include(po => po.CostCenter)
                .AsSplitQuery()
                .AsNoTracking();

            if (request.Status.HasValue)
            {
                operationalOrdersQuery = operationalOrdersQuery
                    .Where(po => po.Status == request.Status);
            }

            if (!string.IsNullOrEmpty(request.CustomerCif))
            {
                operationalOrdersQuery = operationalOrdersQuery
                    .Where(po => po.Customer.Cif == request.CustomerCif);
            }

            var totalRecords = await operationalOrdersQuery.CountAsync(cancellationToken);

            var operationalOrders = await operationalOrdersQuery
                .OrderByDescending(purs => purs.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var operationalOrdersMapped = _mapper.Map<List<OperationalOrderDto>>(operationalOrders);

            return new PagedResponse<OperationalOrderDto>(
                operationalOrdersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );     
        }
    }   
}