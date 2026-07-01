using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Scales.v1.Queries
{
    public class GetWeightFromTheScaleQuery : IRequest<decimal>
    {
        public Guid CompanyId { get; set; }   
    }
}
