using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Purchasing.Services
{
    public static class SupplierPaymentMethodConsistency
    {
        public const string ErrorCode = "ERP:INCONSISTENT_SUPPLIER_PAYMENT_METHOD";

        public const string ErrorMessage =
            "Todas las cotizaciones del mismo proveedor deben usar el mismo método de pago dentro de la solicitud.";

        public static bool IsConsistent(IEnumerable<PaymentMethodType> methods)
            => methods.Distinct().Take(2).Count() <= 1;

        public static bool TryGetSingle(IEnumerable<PaymentMethodType> methods, out PaymentMethodType method)
        {
            var distinct = methods.Distinct().Take(2).ToList();
            if (distinct.Count != 1)
            {
                method = default;
                return false;
            }

            method = distinct[0];
            return true;
        }
    }
}
