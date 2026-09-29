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
    public class GetServiceOrdersRequisitionsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper mapper) : BaseValidatorHandler<GetServiceOrderRequisitionsQuery, PagedResponse<ServiceOrderRequisitionDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<ServiceOrderRequisitionDto>> Handle(GetServiceOrderRequisitionsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var serviceOrdersRequisitionsQuery = _unitOfWork.ServicesOrdersRequisitions.Entities
                .Include(osr => osr.User)
                .Include(osr => osr.ServicesOrder)
                .Where(osr => osr.IsActive)
                .Where(osr => osr.ServiceOrderId == request.ServiceOrderId)
                .Where(osr => osr.ServicesOrder.OperationalOrderId == request.OperationalOrderId)
                .AsNoTracking();

            var totalRecords = await serviceOrdersRequisitionsQuery.CountAsync(cancellationToken);

            var serviceOrders = await serviceOrdersRequisitionsQuery
                .OrderByDescending(so => so.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var serviceOrdersMapped = mapper.Map<List<ServiceOrderRequisitionDto>>(serviceOrders);

            return new PagedResponse<ServiceOrderRequisitionDto>(
                serviceOrdersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
