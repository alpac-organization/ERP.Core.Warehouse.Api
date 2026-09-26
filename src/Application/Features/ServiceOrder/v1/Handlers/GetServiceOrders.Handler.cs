
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Handlers
{
    public class GetServiceOrdersHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<GetServiceOrdersQuery, PagedResponse<ServiceOrderDto>>(unitOfWork, errorManager)
    {
        public override async Task<PagedResponse<ServiceOrderDto>> Handle(GetServiceOrdersQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }
            
            var serviceOrdersQuery = _unitOfWork.ServicesOrders.Entities
                .Include(os => os.OperationalService)
                .Where(os => os.OperationalOrderId == request.OperationalOrderId)
                .AsNoTracking();

            var totalRecords = await serviceOrdersQuery.CountAsync(cancellationToken);

            var serviceOrders = await serviceOrdersQuery
                .OrderByDescending(so => so.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var serviceOrdersMapped = mapper.Map<List<ServiceOrderDto>>(serviceOrders);

            return new PagedResponse<ServiceOrderDto>(
                serviceOrdersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
