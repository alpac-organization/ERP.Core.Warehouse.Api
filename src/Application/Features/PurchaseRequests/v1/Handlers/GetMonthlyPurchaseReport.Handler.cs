using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetMonthlyPurchaseReportHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) 
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
                .Where(pri => pri.PurchaseRequest.IsActive && pri.PurchaseRequest.DeletedAt == null)
                .Where(pri => pri.PurchaseRequest.Branch.CompanyId == request.CompanyId)
                .Where(pri => pri.PurchaseRequest.RequestStatus == PurchaseRequestStatus.Approved)
                .Where(pri => pri.PurchaseRequest.RequestType == PurchaseRequestType.Monthly)
                .Where(pri => pri.PurchaseRequest.RequestDate >= DateOnly.FromDateTime(firstDayOfMonth) &&
                              pri.PurchaseRequest.RequestDate < DateOnly.FromDateTime(firstDayOfNextMonth))
                .Where(pri => pri.Quotations.Any(q =>
                    q.IsActive &&
                    q.DeletedAt == null &&
                    q.IsAcceptedForPurchase))
                .AsNoTracking()
                .AsSplitQuery();

            if (access.Role?.RoleType != RoleType.Administrator && access.Role?.RoleType != RoleType.Supervisor)
            {
                if (access.Role?.RoleType == RoleType.Operator)
                {
                    query = query.Where(pri => pri.PurchaseRequest.RegisteredByUserId == request.UserId);
                }

                if (access.Role?.RoleType == RoleType.Manager)
                {
                    query = query
                        .Where(pri => pri.PurchaseRequest.BranchId == access.Profile.BranchId)
                        .Where(pri => pri.PurchaseRequest.CostCenterId == access.Profile.CostCenterId);
                }
            }

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
