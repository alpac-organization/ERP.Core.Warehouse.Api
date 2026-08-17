using MediatR;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.ServiceOrders;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ServiceOrderController(IMediator _mediator, IMapper _mapper) : ApiControllerBase
{
    [Tags("Ordenes de servicios")]
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

    [Tags("Ordenes de servicios")]
    [HttpGet("companies/{company_id}/modules/{module_code}/service-orders")]
    [ProducesResponseType(typeof(PagedResponse<ServiceOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<ServiceOrderDto>> GetServiceOrdersAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
        [FromQuery] string? code = null,
        [FromQuery] string? cif  = null,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10
    )
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetServiceOrdersQuery()
        {
            Code        = code,
            CustomerCif = cif,
            CompanyId   = company_id,
            ModuleCode  = module_code,
            UserId      = Guid.Parse(userIdStr ?? ""),
            PageNumber  = page_number,
            PageSize    = page_size,
        });
    }

}