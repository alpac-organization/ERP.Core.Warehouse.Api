namespace ERP.Core.Warehouse.Api.Domain.Enums
{
    /// <summary>
    /// Determina el tipo de agrupación del consolidado mensual de compras.
    /// </summary>
    public enum PurchaseRequestConsolidationType
    {
        /// <summary>Agrupa los productos por área solicitante.</summary>
        ByArea = 1,

        /// <summary>Agrupa y suma los productos en total, sin separar por área.</summary>
        TotalProducts = 2
    }
}
