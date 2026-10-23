namespace ERP.Core.Warehouse.Api.Domain.Enums
{
    public enum AnnulmentScope
    {
        // Anula únicamente la cotización seleccionada y devuelve la solicitud a compras  para re-cotizar.
        QuotationOnly = 1,

        // Anula definitivamente todo el trámite de compra.
        FullProcess = 2
    }
}
