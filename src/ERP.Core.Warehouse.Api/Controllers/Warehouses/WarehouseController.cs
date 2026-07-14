using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses
{
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class WarehouseController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Almacenes")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/warehouse")]      
        [ProducesResponseType(typeof(decimal), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<CreatedResult> RegisterWarehouseAsync([FromRoute] Guid company_id, [FromRoute] string module_code)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            //Your Code here

            return Created();
        }

        [Tags("Almacenes")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse")]      
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<List<object>> GetWarehouseAsync([FromRoute] Guid company_id, [FromRoute] string module_code)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            //Your Code here

            return [];
        }
    }
}