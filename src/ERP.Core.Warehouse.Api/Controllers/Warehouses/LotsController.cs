using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class LotsController(IMediator _mediator) : ApiControllerBase
{
    #region Get Lots By Section
    [Tags("Tramos")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots")]
    [ProducesResponseType(typeof(PagedResponse<LotListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<LotListItemDto>> GetLotsBySectionAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid sections_id,
        [FromQuery] string? code = null,
        [FromQuery] RackStatus? status = null,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetLotsBySectionQuery
        {
            WarehouseId = warehouse_id,
            SectionId = sections_id,
            Code = code,
            RackStatus = status,
            UserId = Guid.Parse(userIdStr ?? ""),
            CompanyId = company_id,
            ModuleCode = module_code,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }
    #endregion

    #region Register Lot
    [Tags("Tramos")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots")]
    [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> RegisterLotAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid warehouse_id,
    [FromRoute] Guid sections_id,
    [FromBody] RegisterLotCommand commandLot,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        commandLot.CompanyId = company_id;
        commandLot.ModuleCode = module_code;
        commandLot.UserId = Guid.Parse(userIdStr ?? "");
        commandLot.WarehouseId = warehouse_id;
        commandLot.SectionId = sections_id;

        await _mediator.Send(commandLot, cancellationToken);

        return Created();
    }
    #endregion

    #region Get Lot Capacities
    [Tags("Tramos")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}/capacities")]
    [ProducesResponseType(typeof(LotCapacitiesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<LotCapacitiesDto> GetLotCapacitiesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid sections_id,
        [FromRoute] Guid lot_id,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetLotCapacitiesQuery
        {
            WarehouseId = warehouse_id,
            SectionId = sections_id,
            LotId = lot_id,
            UserId = Guid.Parse(userIdStr ?? ""),
            CompanyId = company_id,
            ModuleCode = module_code
        }, cancellationToken);
    }
    #endregion

    #region Update Lot
    [Tags("Tramos")]
    [HttpPatch("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}")]
    [ProducesResponseType(typeof(OkResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkResult> UpdateLotAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid sections_id,
        [FromRoute] Guid lot_id,
        [FromBody] UpdateLotCommand commandLot,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        commandLot.CompanyId = company_id;
        commandLot.ModuleCode = module_code;
        commandLot.UserId = Guid.Parse(userIdStr ?? "");
        commandLot.WarehouseId = warehouse_id;
        commandLot.SectionId = sections_id;
        commandLot.LotId = lot_id;

        await _mediator.Send(commandLot, cancellationToken);

        return Ok();
    }
    #endregion

    #region Delete Lot
    [Tags("Tramos")]
    [HttpDelete("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}")]
    [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<NoContentResult> DeleteLotAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromRoute] Guid sections_id,
        [FromRoute] Guid lot_id,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        await _mediator.Send(new DeleteLotCommand
        {
            WarehouseId = warehouse_id,
            SectionId = sections_id,
            LotId = lot_id,
            UserId = Guid.Parse(userIdStr ?? ""),
            CompanyId = company_id,
            ModuleCode = module_code
        }, cancellationToken);

        return NoContent();
    }
    #endregion
}