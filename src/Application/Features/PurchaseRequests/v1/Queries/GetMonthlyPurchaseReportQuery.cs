using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries
{
    public class GetMonthlyPurchaseReportQuery : BaseRequest, IRequest<List<Dtos.MonthlyPurchaseReportItemDto>>
    {
        // Mes a filtrar.
        public int Month { get; set; }

        // Año a filtrar.
        public int Year { get; set; }
    }
}