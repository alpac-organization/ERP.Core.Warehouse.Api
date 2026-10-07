using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Queries;


namespace ERP.Core.Warehouse.Api.Controllers.Customers
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class CustomersController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Clientes")]
        [HttpGet("companies/{company_id}/modules/{module_code}/customers")]
        [ProducesResponseType(typeof(PagedResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<CustomerDto>> RegisterCustomerAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
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
    }
}
