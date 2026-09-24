using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class RacksController(IMediator _mediator) : ApiControllerBase
{
    #region 1. Listado General de Racks (Tabla y Plano 2D)
    [Tags("Racks")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks")]
    [ProducesResponseType(typeof(PagedResponse<RackListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<RackListDto>> GetRacksBySectionAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid section_id,
        [FromQuery] string? code = null,
        [FromQuery] int? row_number = null,
        [FromQuery] int? level_number = null,
        [FromQuery] RackStatus? status = null,
        [FromQuery] RackUsageProfile? usage_profile = null,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 24,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetRacksBySectionQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            WarehouseId = warehouse_id,
            SectionId = section_id,
            Code = code,
            RowNumber = row_number,
            LevelNumber = level_number,
            Status = status,
            UsageProfile = usage_profile,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }
    #endregion

    #region 2. Detalle Individual de Rack (Corte de Elevación y Posiciones)
    [Tags("Racks")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks/{rack_id}/details")]
    [ProducesResponseType(typeof(RackDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<RackDetailsDto> GetRackDetailsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid section_id,
        [FromRoute] Guid rack_id,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetRackDetailsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            WarehouseId = warehouse_id,
            SectionId = section_id,
            RackId = rack_id
        }, cancellationToken);
    }
    #endregion

    #region 3. Creación Masiva (Bulk) de Racks
    [Tags("Racks")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks")]
    [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> RegisterRacksBulkAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid section_id,
        [FromBody] RegisterRacksBulkCommand payload,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        payload.CompanyId = company_id;
        payload.ModuleCode = module_code;
        payload.UserId = Guid.Parse(userIdStr ?? "");
        payload.WarehouseId = warehouse_id;
        payload.SectionId = section_id;

        await _mediator.Send(payload, cancellationToken);

        return Created();
    }
    #endregion

    #region 4. Actualización Integral de Rack (Medidas, Estado, Coordenadas)
    [Tags("Racks")]
    [HttpPatch("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks/{rack_id}")]
    [ProducesResponseType(typeof(OkResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkResult> UpdateRackAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid section_id,
        [FromRoute] Guid rack_id,
        [FromBody] UpdateRackCommand payload,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        payload.CompanyId = company_id;
        payload.ModuleCode = module_code;
        payload.UserId = Guid.Parse(userIdStr ?? "");
        payload.WarehouseId = warehouse_id;
        payload.SectionId = section_id;
        payload.RackId = rack_id;

        await _mediator.Send(payload, cancellationToken);

        return Ok();
    }
    #endregion

    #region 5. Eliminación Lógica de Rack
    [Tags("Racks")]
    [HttpDelete("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks/{rack_id}")]
    [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<NoContentResult> DeleteRackAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid section_id,
        [FromRoute] Guid rack_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        await _mediator.Send(new DeleteRackCommand
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            WarehouseId = warehouse_id,
            SectionId = section_id,
            RackId = rack_id
        }, cancellationToken);

        return NoContent();
    }
    #endregion
}
