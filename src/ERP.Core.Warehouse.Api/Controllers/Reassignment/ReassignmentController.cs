using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.Reassignment;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ReassignmentController(IMediator mediator) : ApiControllerBase
{
    #region Issue 1 - Abrir sesión
    [Tags("Reasignamiento")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/reassignment-sessions")]
    [ProducesResponseType(typeof(ReassignmentSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenReassignmentSessionAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var command = new OpenReassignmentSessionCommand
        {
            WarehouseId = warehouse_id,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(command, cancellationToken);
        return Created(string.Empty, response);
    }
    #endregion

    #region Issue 2 - Levantar polines
    [Tags("Reasignamiento")]
    [HttpPost("companies/{company_id}/modules/{module_code}/reassignment-sessions/{session_id}/lift")]
    [ProducesResponseType(typeof(List<ReassignmentMemoryItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LiftStockToMemoryAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid session_id,
        [FromBody] List<LiftStockItemDto> items,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var command = new LiftStockToMemoryCommand
        {
            SessionId = session_id,
            Items = items,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(command, cancellationToken);
        return Created(string.Empty, response);
    }
    #endregion

    #region Issue 3 - Posiciones disponibles
    [Tags("Reasignamiento")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/available-positions")]
    [ProducesResponseType(typeof(List<AvailablePositionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailablePositionsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_id,
        [FromQuery] Guid? section_id,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var query = new GetAvailablePositionsQuery
        {
            WarehouseId = warehouse_id,
            SectionId = section_id,
            Status = status,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(query, cancellationToken);
        return Ok(response);
    }
    #endregion

    #region Issue 4 - Confirmar polín en aire
    [Tags("Reasignamiento")]
    [HttpPost("companies/{company_id}/modules/{module_code}/reassignment-sessions/{session_id}/memory-items/{memory_item_id}/resolve")]
    [ProducesResponseType(typeof(ReassignmentMemoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResolveMemoryItemAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid session_id,
        [FromRoute] Guid memory_item_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var command = new ResolveMemoryItemCommand
        {
            SessionId = session_id,
            MemoryItemId = memory_item_id,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Tags("Reasignamiento")]
    [HttpPost("companies/{company_id}/modules/{module_code}/reassignment-sessions/{session_id}/memory-items/resolve")]
    [ProducesResponseType(typeof(List<ReassignmentMemoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResolveMemoryItemsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid session_id,
        [FromBody] List<Guid> memory_item_ids,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var command = new ResolveMemoryItemsCommand
        {
            SessionId = session_id,
            MemoryItemIds = memory_item_ids,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }
    #endregion

    #region Issue 5 - Pausar Session
    [Tags("Reasignamiento")]
    [HttpPost("companies/{company_id}/modules/{module_code}/reassignment-session/{session_id}/pause")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PauseSessionAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid session_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

        var command = new PauseSessionCommand
        {
            SessionId = session_id,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }
    #endregion

    #region Issue 6 - Cerrar sesión
    [Tags("Reasignamiento")]
    [HttpPost("companies/{company_id}/modules/{module_code}/reassignment-sessions/{session_id}/close")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CloseReassignmentSessionAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid session_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var command = new CloseReassignmentSessionCommand
        {
            SessionId = session_id,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }
    #endregion
}