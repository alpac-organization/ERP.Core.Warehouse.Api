using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
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
        IPurchaseRequestVisibilityService _visibilityService)
        : BaseValidatorHandler<GetMonthlyPurchaseReportQuery, List<MonthlyPurchaseReportItemDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<List<MonthlyPurchaseReportItemDto>> Handle(GetMonthlyPurchaseReportQuery request, CancellationToken cancellationToken)
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
                    .Where(pr => pr.RequestStatus == PurchaseRequestStatus.Approved
                              || pr.RequestStatus == PurchaseRequestStatus.Finished)
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
                .Where(pri => pri.DeletedAt == null)
                .Where(pri => visibleRequestIds.Contains(pri.PurchaseRequestId))
                .AsNoTracking()
                .AsSplitQuery();

            var purchaseRequestItems = await query.ToListAsync(cancellationToken);

            return purchaseRequestItems.Select(item =>
            {
                var acceptedQuotation = item.Quotations.FirstOrDefault();

                return new MonthlyPurchaseReportItemDto
                {
                    Month = request.Month,
                    Year = request.Year,
                    Key = reportKey,
                    RequestType = item.PurchaseRequest.RequestType,
                    BranchName = item.PurchaseRequest.Branch?.BranchName,
                    AreaName = item.PurchaseRequest.WorkArea?.WorkAreaName ?? item.PurchaseRequest.WorkArea?.Description,
                    SupplierName = acceptedQuotation?.Supplier?.SuppliersLegalName,
                    Description = item.Product?.ProductName ?? item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = acceptedQuotation?.PriceUnit ?? 0m,
                    TotalPrice = acceptedQuotation?.PriceTotal ?? 0m
                };
            }).ToList();
        }
    }
}
