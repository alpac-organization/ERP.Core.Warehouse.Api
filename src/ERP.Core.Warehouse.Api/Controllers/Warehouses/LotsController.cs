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
    [ProducesResponseType(typeof(bool), StatusCodes.Status201Created)]
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

        var response = await _mediator.Send(commandLot, cancellationToken);

        return Created(string.Empty, response);
    }
    #endregion
}