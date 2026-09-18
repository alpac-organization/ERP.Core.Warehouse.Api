namespace ERP.Core.Warehouse.Api.Domain.Enums
{
    /// <summary>
    /// Determina el alcance de la anulación o retorno en el flujo de compras.
    /// </summary>
    public enum AnnulmentScope
    {
        /// <summary>
        /// Anula únicamente la cotización seleccionada y devuelve la solicitud a compras (sigue viva para re-cotizar).
        /// </summary>
        QuotationOnly = 1,

        /// <summary>
        /// Anula definitivamente todo el trámite de compra.
        /// </summary>
        FullProcess = 2
    }
}
