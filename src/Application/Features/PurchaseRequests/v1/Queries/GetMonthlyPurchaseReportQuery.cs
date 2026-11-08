using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries
{
    public class GetMonthlyPurchaseReportQuery : BaseRequest, IRequest<MonthlyPurchaseReportDto>
    {
        public int Month { get; set; }

        public int Year { get; set; }
    }
}