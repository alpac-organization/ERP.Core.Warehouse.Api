using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.PurchaseRequests
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class PurchaseRequestController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Solicitudes de compras")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/purchase-requests")]      
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<CreatedResult> RegisterPurchaseRequestCommand([FromRoute] Guid company_id, [FromRoute] string module_code, [FromBody] RegisterPurchaseRequestCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");

            await _mediator.Send(payload);

            return Created();
        }

        [Tags("Solicitudes de compras")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-requests")]      
        [ProducesResponseType(typeof(PagedResponse<QuotationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<OkResult> GetPurchaseRequestAsync([FromRoute] Guid company_id, [FromRoute] string module_code)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return Ok();
        }
    }
}