using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;

namespace ERP.Core.Warehouse.Api.Controllers.PurchaseOrders
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class QuotationController(/*IMediator _mediator*/) : ApiControllerBase
    {
        [Tags("Ordenes de compras")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-orders")]      
        [ProducesResponseType(typeof(OkResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<OkResult> GetPurchaseOrdersAsync([FromRoute] Guid company_id, [FromRoute] string module_code)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            //Your code here
            

            return Ok();
        }


        [Tags("Ordenes de compras")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/purchase-orders/{purchase_order_id}/process")]      
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<CreatedResult> RegisterQuoteAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_order_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            // your code here.


            return Created();
        }

    }
}