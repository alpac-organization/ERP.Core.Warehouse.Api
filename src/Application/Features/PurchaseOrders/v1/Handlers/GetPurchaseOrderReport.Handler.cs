using AutoMapper;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetPurchaseOrderReportHandler(
        IUnitOfWork unitOfWork,
        IErrorManager errorManager,
        IMapper mapper,
        IPurchaseRequestVisibilityService visibilityService)
        : BaseValidatorHandler<GetPurchaseOrderReportQuery, PurchaseOrderReportDto>(unitOfWork, errorManager)
    {
        public override async Task<PurchaseOrderReportDto> Handle(
            GetPurchaseOrderReportQuery request,
            CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var visibleRequestIds = visibilityService.Apply(
                    _unitOfWork.PurchaseRequests.Entities,
                    access.Role?.RoleType,
                    request.UserId,
                    access.Profile?.AreaId,
                    access.Profile?.CostCenterId,
                    access.Profile?.BranchId)
                .Select(purchaseRequest => purchaseRequest.Id);

            var purchaseOrder = await _unitOfWork.PurchaseOrders.Entities
                .Include(order => order.Supplier)
                    .ThenInclude(supplier => supplier!.SupplierDetails)
                .Include(order => order.PaymentRequest)
                .Include(order => order.PurchaseOrderItems)
                    .ThenInclude(item => item.Product)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.Branch)
                        .ThenInclude(branch => branch.Company)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.WorkArea)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.RegistrationUser)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(quotation =>
                            quotation.IsActive &&
                            quotation.DeletedAt == null &&
                            quotation.IsAcceptedForPurchase))
                            .ThenInclude(quotation => quotation.Supplier)
                .Include(order => order.PurchaseRequest)
                    .ThenInclude(purchaseRequest => purchaseRequest.PurchaseRequestItems)
                        .ThenInclude(item => item.Quotations.Where(quotation =>
                            quotation.IsActive &&
                            quotation.DeletedAt == null &&
                            quotation.IsAcceptedForPurchase))
                            .ThenInclude(quotation => quotation.SupplierProduct)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(order => order.Id == request.PurchaseOrderId)
                .Where(order => order.DeletedAt == null)
                .Where(order => visibleRequestIds.Contains(order.PurchaseRequestId))
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowNotFound<PurchaseOrderReportDto>(
                    "No se encontro la orden de compra",
                    "ERP:NOT_FOUND");
            }

            if (string.IsNullOrWhiteSpace(purchaseOrder.Code))
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderReportDto>(
                    "La orden de compra no tiene código.",
                    "ERP:PURCHASE_ORDER_CODE_REQUIRED");
            }

            var header = mapper.Map<PurchaseOrderTemplateDto>(purchaseOrder);
            var supplierQuotations = PurchaseOrderReportLines.AcceptedQuotations(purchaseOrder);
            if (!PurchaseOrderReportLines.TryResolveCurrency(supplierQuotations, out var currency))
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderReportDto>(
                    "La orden de compra tiene líneas en monedas distintas.",
                    "ERP:INCONSISTENT_PURCHASE_ORDER_CURRENCY");
            }

            var items = PurchaseOrderReportLines.BuildItems(purchaseOrder, supplierQuotations);
            var serviceAmount = items.Sum(item => item.PriceTotal);
            var ivaAmount = items.Sum(item => item.Iva);
            var culture = new CultureInfo("es-NI");
            const string title = "Orden de compra";

            return new PurchaseOrderReportDto
            {
                PurchaseOrderId = purchaseOrder.Id,
                PurchaseOrderCode = purchaseOrder.Code,
                PaymentCondition = ResolvePaymentCondition(purchaseOrder.Supplier),
                PaymentRequestCode = purchaseOrder.PaymentRequest?.Code,
                Title = title,
                Concept = header.Concept ?? purchaseOrder.PurchaseRequest.Concept,
                SentByUserInformation = header.SentByUserInformation,
                CompanyInformation = header.CompanyInformation,
                Items = items,
                DocumentInfo = BuildDocumentInfo(purchaseOrder, title, culture),
                PaymentInfo = new PurchaseOrderReportPaymentInfo
                {
                    Department = purchaseOrder.PurchaseRequest.WorkArea?.WorkAreaName
                        ?? purchaseOrder.PurchaseRequest.WorkArea?.Description,
                    Supplier = purchaseOrder.Supplier?.SuppliersLegalName
                        ?? supplierQuotations.FirstOrDefault()?.Supplier?.SuppliersLegalName,
                    Customer = purchaseOrder.PurchaseRequest.Branch.Company?.CompanieName,
                    Currency = currency,
                    ServiceAmount = serviceAmount,
                    Iva = ivaAmount,
                    IncomeTax = supplierQuotations.Sum(quotation => quotation.Ir),
                    NetToPay = serviceAmount + ivaAmount
                }
            };
        }

        private static PaymentCondition ResolvePaymentCondition(Supplier? supplier)
        {
            return supplier?.SupplierDetails?.HasCredit == true
                ? PaymentCondition.Credit
                : PaymentCondition.Cash;
        }

        private static PurchaseOrderReportDocumentInfo BuildDocumentInfo(
            PurchaseOrder purchaseOrder,
            string title,
            CultureInfo culture)
        {
            var priority = purchaseOrder.PurchaseRequest.PriorityLevel;

            return new PurchaseOrderReportDocumentInfo
            {
                Title = title,
                RequestCode = purchaseOrder.Code,
                Date = DateTime.Now.ToString("dd/MM/yyyy", culture),
                IsCritical = priority == PriorityLevel.Critical,
                IsNormal = priority is PriorityLevel.None or PriorityLevel.Normal,
                IsUnforeseen = priority == PriorityLevel.Unforeseen
            };
        }
    }
}
