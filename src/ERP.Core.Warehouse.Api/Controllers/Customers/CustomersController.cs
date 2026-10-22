using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Commands;


namespace ERP.Core.Warehouse.Api.Controllers.Customers
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    [Tags("Clientes")]
    public class CustomersController(IMediator _mediator) : ApiControllerBase
    {
        [HttpGet("companies/{company_id}/modules/{module_code}/customers")]
        [ProducesResponseType(typeof(PagedResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<CustomerDto>> GetCustomerAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetCustomersQuery()
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                PageNumber = page_number,
                PageSize = page_size
            });
        }

        [HttpPost("companies/{company_id}/modules/{module_code}/customers")]
        [ProducesResponseType(typeof(RegisterCustomerResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterCustomerAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromBody] RegisterCustomerCommand command,
            CancellationToken ct
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            command.CompanyId   = company_id;
            command.ModuleCode  = module_code;
            command.UserId      = Guid.Parse(userIdStr ?? "");

            var result = await _mediator.Send(command, ct);

            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
