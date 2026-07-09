using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Core.Warehouse.Api.Controllers.ServiceOrders;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ServiceOrderController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Service Orders")]
    [HttpPost("companies/{company_id}/service-orders")]
    [ProducesResponseType(typeof(CreateServiceOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateServiceOrderResponse>> CreateServiceOrderAsync(
        [FromRoute] Guid company_id,
        [FromBody] CreateServiceOrderCommand command)
    {
        command.CompanyId = company_id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}