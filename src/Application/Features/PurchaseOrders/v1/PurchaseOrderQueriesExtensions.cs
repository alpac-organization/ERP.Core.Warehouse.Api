using System.Linq;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1
{
    public static class PurchaseOrderQueriesExtensions
    {
        public static IQueryable<PurchaseOrder> IncludePurchaseOrderHierarchy(this IQueryable<PurchaseOrder> query)
        {
            return query
                .Include(purs => purs.SentByUser)
                .Include(purs => purs.ReviewedByUser)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)
                        .ThenInclude(branch => branch.Company)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                        .ThenInclude(area => area.CostCenters)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)
                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure);
        }
    }
}
