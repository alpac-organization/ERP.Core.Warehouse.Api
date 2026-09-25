
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Queries;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;

namespace ERP.Core.Warehouse.Api.Controllers.OperationalOrders
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class OperationalOrdersController(IMediator _mediator) : ApiControllerBase
    {

        [Tags("Solicitudes de compras")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/operational-orders")]
        [ProducesResponseType(typeof(PagedResponse<OperationalOrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<OperationalOrderDto>> GetOperationalOdersAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] string? code                      = null,
            [FromQuery] string? customer_cif              = null,
            [FromQuery] OperationalOrderStatus? status    = null,
            [FromQuery] int page_number                   = 1,
            [FromQuery] int page_size                     = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetOperationalOrdersQuery()
            {
                CompanyId   = company_id,
                Status      = status,
                PoCode      = code,
                CustomerCif = customer_cif,
                ModuleCode  = module_code,
                PageNumber  = page_number,
                PageSize    = page_size,
                UserId      = Guid.Parse(userIdStr ?? ""),
            });
        }
    }
}
