using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Scales.v1.Queries;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.Scales.v1.Handlers
{
    public class GetWeightFromTheScaleHandler(IScaleServices _scaleServices) : IRequestHandler<GetWeightFromTheScaleQuery, decimal>
    {
        public async Task<decimal> Handle(GetWeightFromTheScaleQuery request, CancellationToken cancellationToken)
        {
            var weight = await _scaleServices.GetWeightFromTheScale();

            //Realizar registro del peso inicial

            return weight;
        }
    }
}