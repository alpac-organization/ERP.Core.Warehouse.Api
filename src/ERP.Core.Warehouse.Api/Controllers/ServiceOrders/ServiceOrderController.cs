using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.ServiceOrders
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class ServiceOrderController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Ordenes de servicios")]
        [HttpPost("companies/{company_id}/branches/{branch_id}/modules/{module_code}/operational-orders/{operational_order_id}/services-orders")]
        [ProducesResponseType(typeof(CreateServiceOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<CreatedResult> CreateServiceOrderAsync([FromRoute] Guid company_id, [FromRoute] Guid branch_id, [FromRoute] string module_code, [FromRoute] Guid operational_order_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            //Your Code here, esto asignara un servicio aderido a un (PO)

            return Created();
        }

        [Tags("Ordenes de servicios")]
        [HttpGet("companies/{company_id}/branches/{branch_id}/modules/{module_code}/operational-orders/{operational_order_id}/services-orders")]
        [ProducesResponseType(typeof(PagedResponse<ServiceOrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<ServiceOrderDto>> GetServiceOrdersAsync([FromRoute] Guid company_id, [FromRoute] Guid branch_id, [FromRoute] string module_code, [FromRoute] Guid operational_order_id,
            [FromQuery] int page_size = 10,
            [FromQuery] int page_number = 1        
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetServiceOrdersQuery()
            {
                CompanyId   = company_id,
                ModuleCode  = module_code,
                PageNumber  = page_number,
                PageSize    = page_size,
                UserId      = Guid.Parse(userIdStr ?? ""),
                OperationalOrderId = operational_order_id,
            });
        }

    }
}