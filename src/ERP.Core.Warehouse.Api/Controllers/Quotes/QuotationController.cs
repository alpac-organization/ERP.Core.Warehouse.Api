using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Queries;
using MediatR;

namespace ERP.Core.Warehouse.Api.Controllers.Quotes
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class QuotationController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Contizaciones")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/quotations")]      
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<CreatedResult> RegisterQuoteAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromBody] RegisterQuotationCommand pyaload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            
            pyaload.CompanyId = company_id;
            pyaload.ModuleCode = module_code;
            pyaload.UserId = Guid.Parse(userIdStr ?? "");

            await _mediator.Send(pyaload);

            return Created();
        }
        
        [Tags("Contizaciones")] 
        [HttpPatch("companies/{company_id}/modules/{module_code}/quotations/{quotation_id}")]      
        [ProducesResponseType(typeof(OkResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<OkResult> UpdateQuoteAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid quotation_id, [FromBody] UpdateQuotationCommand pyaload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            
            pyaload.CompanyId = company_id;
            pyaload.ModuleCode = module_code;
            pyaload.QuotationId = quotation_id;
            pyaload.UserId = Guid.Parse(userIdStr ?? "");

            await _mediator.Send(pyaload);

            return Ok();
        }

        [Tags("Contizaciones")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-request-items/{purchase_request_item_id}/quotations")]      
        [ProducesResponseType(typeof(PagedResponse<QuotationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<PagedResponse<QuotationDto>> GetQuotationsAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid purchase_request_item_id,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetQuotationsQuery()
            {
                PurchaseRequestItemId = purchase_request_item_id,
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                PageNumber = page_number,
                PageSize = page_size,
            });
        }
    }
}
