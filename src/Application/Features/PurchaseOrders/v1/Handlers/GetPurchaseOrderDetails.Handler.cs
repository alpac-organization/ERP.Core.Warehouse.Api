using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

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
                .Include(order => order.PaymentRequest)
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
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.CostCenter)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.AccountingReview)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.ManagementReview)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                        .ThenInclude(user => user.Profiles
                            .Where(profile => profile.CompanyId == profileCompanyId)
                            .Take(1))
                        .ThenInclude(profile => profile.WorkArea)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)
                        .ThenInclude(user => user!.Profiles
                            .Where(profile => profile.CompanyId == profileCompanyId)
                            .Take(1))
                        .ThenInclude(profile => profile.WorkArea)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.AnnulledByUser)
                        .ThenInclude(user => user!.Profiles
                            .Where(profile => profile.CompanyId == profileCompanyId)
                            .Take(1))
                        .ThenInclude(profile => profile.WorkArea)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.ReasonRejection)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                            .ThenInclude(product => product.Category)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                            .ThenInclude(product => product.SupplierProducts.Where(sp => sp.IsActive && sp.DeletedAt == null))
                                .ThenInclude(sp => sp.Supplier)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                            .ThenInclude(product => product.SupplierProducts.Where(sp => sp.IsActive && sp.DeletedAt == null))
                                .ThenInclude(sp => sp.TierPrices.Where(tier => tier.DeletedAt == null))
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(q => q.IsActive && q.DeletedAt == null))
                            .ThenInclude(q => q.Supplier)
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
            mapped.PurchaseRequest.IsPurchaseOrderGenerated = true;
            QuotationRecommendations.Apply(mapped.PurchaseRequest.PurchaseRequestItems);
            return mapped;
        }
    }
}
