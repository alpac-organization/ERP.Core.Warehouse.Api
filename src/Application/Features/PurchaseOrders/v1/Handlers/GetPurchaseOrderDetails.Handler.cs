using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetPurchaseOrderDetailsHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        IMapper _mapper,
        IPurchaseRequestVisibilityService _visibilityService)
        : BaseValidatorHandler<GetPurchaseOrderDetailsQuery, PurchaseOrderDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseOrderDetailsDto> Handle(GetPurchaseOrderDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var profileCompanyId = access.Profile!.CompanyId;

            var visibleRequestIds = _visibilityService.Apply(
                    _unitOfWork.PurchaseRequests.Entities,
                    access.Role?.RoleType,
                    request.UserId,
                    access.Profile?.AreaId,
                    access.Profile?.CostCenterId,
                    access.Profile?.BranchId)
                .Select(pr => pr.Id);

            var purchaseOrder = await _unitOfWork.PurchaseOrders.Entities
                .Include(order => order.Supplier)
                .Include(order => order.SentByUser)
                    .ThenInclude(user => user.Profiles
                        .Where(profile => profile.CompanyId == profileCompanyId)
                        .Take(1))
                    .ThenInclude(profile => profile.WorkArea)
                .Include(order => order.ReviewedByUser)
                    .ThenInclude(user => user!.Profiles
                        .Where(profile => profile.CompanyId == profileCompanyId)
                        .Take(1))
                    .ThenInclude(profile => profile.WorkArea)
                .Include(order => order.PurchaseOrderItems)
                    .ThenInclude(item => item.Product)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(quotation =>
                            quotation.IsActive &&
                            quotation.DeletedAt == null &&
                            quotation.IsAcceptedForPurchase))
                            .ThenInclude(quotation => quotation.Supplier)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(order => order.Id == request.PurchaseOrderId)
                .Where(order => visibleRequestIds.Contains(order.PurchaseRequestId))
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderDetailsDto>("No se encontro la orden de compra", "ERP:NOT_FOUND");
            }

            var mapped = _mapper.Map<PurchaseOrderDetailsDto>(purchaseOrder);
            mapped.Items = BuildItems(purchaseOrder);
            return mapped;
        }

        private List<PurchaseOrderDetailItemDto> BuildItems(PurchaseOrder purchaseOrder)
        {
            var acceptedQuotations = purchaseOrder.PurchaseRequest.PurchaseRequestItems
                .SelectMany(item => item.Quotations)
                .Where(quotation => quotation.SupplierId == purchaseOrder.SupplierId)
                .GroupBy(quotation => quotation.PurchaseRequestItemId)
                .ToDictionary(group => group.Key, group => group.First());

            return purchaseOrder.PurchaseOrderItems
                .Select(orderItem => new PurchaseOrderDetailItemDto
                {
                    PurchaseOrderItemId = orderItem.Id,
                    PurchaseRequestItemId = orderItem.PurchaseRequestItemId,
                    ProductId = orderItem.ProductId,
                    ProductName = orderItem.Product?.ProductName,
                    ProductCode = orderItem.Product?.Code,
                    Quantity = orderItem.Quantity,
                    UnitPrice = orderItem.UnitPrice,
                    Quotation = ResolveQuotation(orderItem, acceptedQuotations)
                })
                .ToList();
        }

        private PurchaseOrderDetailQuotationDto? ResolveQuotation(
            PurchaseOrderItem orderItem,
            IReadOnlyDictionary<Guid, Quotation> acceptedQuotations)
        {
            if (!orderItem.PurchaseRequestItemId.HasValue
                || !acceptedQuotations.TryGetValue(orderItem.PurchaseRequestItemId.Value, out var quotation))
            {
                return null;
            }

            return new PurchaseOrderDetailQuotationDto
            {
                QuotationId = quotation.Id,
                IsAcceptedForPurchase = quotation.IsAcceptedForPurchase,
                PriceUnit = quotation.PriceUnit,
                PriceTotal = quotation.PriceTotal,
                PaymentMethodType = quotation.PaymentMethodType,
                SupplierId = quotation.SupplierId,
                SupplierInformation = quotation.Supplier is null
                    ? new SupplierInformation()
                    : _mapper.Map<SupplierInformation>(quotation.Supplier)
            };
        }
    }
}
