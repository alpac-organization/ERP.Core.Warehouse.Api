using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Scales.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.Scales
{
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class VacationsController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Bascula")] 
        [HttpGet("companies/{company_id}/scales")]      
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<decimal> GetWeightFromTheScaleAsync([FromRoute] Guid company_id)
        {
            // var userIdStr = HttpContext.Items["UserId"] as string;
            return await _mediator.Send(new GetWeightFromTheScaleQuery()
            {
                CompanyId = company_id
            });
        }
    }
}