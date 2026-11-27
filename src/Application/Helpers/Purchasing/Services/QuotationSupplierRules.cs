using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public static class QuotationSupplierRules
    {
        public const int CriticalRequisitionMinSuppliers = 1;
        public const int DefaultMinSuppliers = 2;

        public static int GetMinRequiredSuppliers(PurchaseRequestType requestType, PriorityLevel priorityLevel)
        {
            return requestType == PurchaseRequestType.Requisition && priorityLevel == PriorityLevel.Critical
                ? CriticalRequisitionMinSuppliers
                : DefaultMinSuppliers;
        }
    }
}
