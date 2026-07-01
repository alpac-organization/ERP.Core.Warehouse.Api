using ERP.Core.Warehouse.Api.Application.Features.Scales.v1.Queries;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Scales.v1.Handlers
{
    public class GetWeightFromTheScaleHandler : IRequestHandler<GetWeightFromTheScaleQuery, decimal>
    {
        public async Task<decimal> Handle(GetWeightFromTheScaleQuery request, CancellationToken cancellationToken)
        {
            

            return 0.0m;
        }
    }
}