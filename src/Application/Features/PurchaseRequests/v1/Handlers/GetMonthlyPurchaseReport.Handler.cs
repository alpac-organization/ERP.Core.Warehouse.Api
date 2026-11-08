using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetMonthlyPurchaseReportHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager,
        IPurchaseRequestVisibilityService _visibilityService,
        IPurchaseTaxService _purchaseTaxService)
        : BaseValidatorHandler<GetMonthlyPurchaseReportQuery, MonthlyPurchaseReportDto>(_unitOfWork, _errorManager)
    {
        public override async Task<MonthlyPurchaseReportDto> Handle(GetMonthlyPurchaseReportQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var firstDayOfMonth = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
            var reportKey = firstDayOfMonth
                .ToString("MMM-yy", CultureInfo.GetCultureInfo("es-ES"))
                .Replace(".", string.Empty)
                .ToLowerInvariant();

            var visibleRequests = _visibilityService.Apply(
                _unitOfWork.PurchaseRequests.Entities
                    .Where(pr => pr.IsActive && pr.DeletedAt == null)
                    .Where(pr => pr.Branch.CompanyId == request.CompanyId)
                    .Where(pr => pr.RequestType == PurchaseRequestType.Monthly)
                    .Where(pr => pr.RequestStatus == PurchaseRequestStatus.Finished)
                    .Where(pr => pr.CreatedAt >= firstDayOfMonth && pr.CreatedAt < firstDayOfNextMonth),
                access.Role?.RoleType,
                request.UserId,
                access.Profile?.AreaId,
                access.Profile?.CostCenterId,
                access.Profile?.BranchId);

            var visibleRequestIds = visibleRequests.Select(pr => pr.Id);

            var query = _unitOfWork.PurchaseRequestItems.Entities
                .Include(pri => pri.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)
                .Include(pri => pri.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                .Include(pri => pri.Product)
                .Include(pri => pri.Quotations.Where(q =>
                    q.IsActive &&
                    q.DeletedAt == null &&
                    q.IsAcceptedForPurchase))
                    .ThenInclude(q => q.Supplier)
                .Include(pri => pri.Quotations.Where(q =>
                    q.IsActive &&
                    q.DeletedAt == null &&
                    q.IsAcceptedForPurchase))
                    .ThenInclude(q => q.SupplierProduct)
                .Where(pri => pri.DeletedAt == null)
                .Where(pri => visibleRequestIds.Contains(pri.PurchaseRequestId))
                .AsNoTracking()
                .AsSplitQuery();

            var purchaseRequestItems = await query.ToListAsync(cancellationToken);
            var rates = await _purchaseTaxService.GetActiveRatesAsync(cancellationToken);
            var exchangeRate = _purchaseTaxService.ResolveExchangeRate(rates);

            var items = purchaseRequestItems
                .Select(item => BuildItem(item, request.Month, request.Year, reportKey))
                .ToList();

            return new MonthlyPurchaseReportDto
            {
                Items = items,
                GrandTotal = items.Sum(item => ToNio(item.TotalPrice, item.Currency, exchangeRate))
            };
        }

        private static MonthlyPurchaseReportItemDto BuildItem(
            PurchaseRequestItem item,
            int month,
            int year,
            string reportKey)
        {
            var acceptedQuotation = item.Quotations.FirstOrDefault();
            var currency = acceptedQuotation?.SupplierProduct?.Currency;
            var unitPrice = acceptedQuotation?.PriceUnit ?? 0m;
            var lineTotal = item.Quantity * unitPrice;

            return new MonthlyPurchaseReportItemDto
            {
                Month = month,
                Year = year,
                Key = reportKey,
                RequestType = item.PurchaseRequest.RequestType,
                BranchName = item.PurchaseRequest.Branch?.BranchName,
                AreaName = item.PurchaseRequest.WorkArea?.WorkAreaName ?? item.PurchaseRequest.WorkArea?.Description,
                SupplierName = acceptedQuotation?.Supplier?.SuppliersLegalName,
                Description = item.Product?.ProductName ?? item.Description,
                Quantity = item.Quantity,
                Currency = currency,
                UnitPrice = unitPrice,
                TotalPrice = lineTotal
            };
        }

        private static decimal ToNio(decimal amount, Currency? currency, decimal exchangeRate)
        {
            if (currency != Currency.USD)
            {
                return amount;
            }

            return amount * exchangeRate;
        }
    }
}
