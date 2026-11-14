using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Commands;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Controllers.FuelRequest;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
[Tags("Solicitudes de Combustible")]
public class FuelRequestsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("companies/{company_id}/modules/{module_code}/inventory/fuel-requests")]
    [ProducesResponseType(typeof(PagedResponse<GetFuelRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<GetFuelRequestDto>> GetFuelRequestsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? code,
        [FromQuery] Guid? branch_id,
        [FromQuery] Guid? machinery_id,
        [FromQuery] Guid? product_stock_id,
        [FromQuery] OutflowStatus? status,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10
    )
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetFuelRequestsQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            Code = code,
            BranchId = branch_id,
            MachineryId = machinery_id,
            ProductStockId = product_stock_id,
            Status = status,
            PageNumber = page_number,
            PageSize = page_size
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/inventory/fuel-requests/{fuel_request_id}/detail")]
    [ProducesResponseType(typeof(GetFuelRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetFuelRequestDetailDto> GetFuelRequestsDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid fuel_request_id
    )
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetFuelRequestsDetailQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            FuelRequestId = fuel_request_id
        });
    }

    [HttpPost("companies/{company_id}/modules/{module_code}/inventory/fuel-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> RegisterFuelRequest(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] RegisterFuelRequestsCommand command)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        command.CompanyId = company_id;
        command.ModuleCode = module_code;
        command.UserId = Guid.Parse(userIdStr ?? "");

        var code = await mediator.Send(command);

        return Created("", code);
    }
}