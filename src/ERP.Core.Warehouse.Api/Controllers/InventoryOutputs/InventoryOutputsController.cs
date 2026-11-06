using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Queries;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Controllers.InventoryOutputs;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
[Tags("Salidas de Inventario")]
public class InventoryOutputsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("companies/{company_id}/modules/{module_code}/inventory/inventory-outputs")]
    [ProducesResponseType(typeof(PagedResponse<GetInventoryOutputDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<GetInventoryOutputDto>> GetInventoryOutputsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? output_code,
        [FromQuery] InventoryOutputsStatus? status,
        [FromQuery] string? request_type,
        [FromQuery] string? supply_request_code,
        [FromQuery] string? fuel_request_code,
        [FromQuery] Guid? reason_inventory_removal_id,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10
    )
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetInventoryOutputsQuery()
        {
            CompanyId                   = company_id,
            ModuleCode                  = module_code,
            UserId                      = Guid.Parse(userIdStr ?? ""),
            OutputCode                  = output_code,
            Status                      = status,
            RequestType                 = request_type,
            SupplyRequestCode           = supply_request_code,
            FuelRequestCode             = fuel_request_code,
            ReasonInventoryRemovalId    = reason_inventory_removal_id,
            PageNumber                  = page_number,
            PageSize                    = page_size
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/inventory/inventory-outputs/{inventory_output_id}/detail")]
    [ProducesResponseType(typeof(GetInventoryOutputDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetInventoryOutputDetailDto> GetInventoryOutputDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid inventory_output_id
    )
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetInventoryOutputsDetailQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            InventoryOutputId = inventory_output_id
        });
    }
}