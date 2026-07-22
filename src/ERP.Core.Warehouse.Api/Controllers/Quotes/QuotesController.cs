using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Quotes
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class QuotesController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Contizaciones")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/quotes")]      
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<CreatedResult> RegisterQuoteAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromBody] RegisterQuoteCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;            
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            
            await _mediator.Send(payload);

            return Created();
        }

        [Tags("Contizaciones")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/quotes")]      
        [ProducesResponseType(typeof(PagedResponse<QuotationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<PagedResponse<QuotationDto>> GetQuoteAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] Guid? branch_id,
            [FromQuery] string? quote_code,

            [FromQuery] int page_size = 10,
            [FromQuery] int page_number = 1
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetQuotesQuery() {
                ModuleCode  = module_code,
                CompanyId   = company_id,
                BranchId    = branch_id,
                QuoteCode   = quote_code,
                PageNumber  = page_number,
                PageSize    = page_size,
                UserId      = Guid.Parse(userIdStr ?? "")
            });
        }
    }
}