using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.ServiceOrders;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ServiceOrderController(IMediator _mediator, IMapper _mapper) : ApiControllerBase
{
    [Tags("Service Orders")]
    [HttpPost("companies/{company_id}/branches/{branch_id}/modules/{module_code}/service-orders")]
    [ProducesResponseType(typeof(CreateServiceOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateServiceOrderResponse>> CreateServiceOrderAsync(
        [FromRoute] Guid company_id, 
        [FromRoute] String module_code, 
        [FromRoute] Guid branch_id, 
        [FromBody] CreateServiceOrderDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = _mapper.Map<CreateServiceOrderCommand>(dto);
        command.UserId = userId;
        command.CompanyId = company_id;
        command.ModuleCode = module_code;
        command.BranchId = branch_id;

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }
}