using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;


using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetPurchaseRequestProductsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetPurchaseRequestsProductsQuery, PagedResponse<PurchaseRequestItemDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<PurchaseRequestItemDto>> Handle(GetPurchaseRequestsProductsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var purchaseRequestItems = await _unitOfWork.PurchaseRequestItems.Entities
                .Where(item => item.PurchaseRequestId == request.PurchaseRequestId)
                .Include(item => item.Product)
                    .ThenInclude(product => product.Category)
                .Include(item => item.UnitMeasure)
                .Include(item => item.Quotations)
                    .ThenInclude(quote => quote.Supplier)
                .ToListAsync(cancellationToken);

            var purchaseRequestItemsMapped = _mapper.Map<List<PurchaseRequestItemDto>>(purchaseRequestItems);

            return new PagedResponse<PurchaseRequestItemDto>(
                purchaseRequestItemsMapped,
                request.PageNumber,
                request.PageSize,
                0
            );
        }
    }
    
}