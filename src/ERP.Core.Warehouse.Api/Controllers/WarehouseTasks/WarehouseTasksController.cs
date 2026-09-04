using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Queries;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Controllers.WarehouseTasks;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class WarehouseTasksController(IMediator mediator) : ApiControllerBase
{
    [Tags("Tareas de bodega")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-tasks")]
    [ProducesResponseType(typeof(List<WarehouseTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<List<WarehouseTaskDto>> GetAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] Guid? warehouse_id,
        [FromQuery] WarehouseTaskStatus? status,
        [FromQuery] WarehouseTaskType? task_type,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            userId = Guid.Empty;
        }

        return await mediator.Send(new GetWarehouseTasksQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            WarehouseId = warehouse_id,
            Status = status,
            TaskType = task_type
        }, cancellationToken);
    }

    [Tags("Tareas de bodega")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-tasks/{warehouse_task_id}/pause")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> PauseAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_task_id,
        CancellationToken cancellationToken)
        => SendAsync(new PauseWarehouseTaskCommand
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            WarehouseTaskId = warehouse_task_id
        }, cancellationToken);

    [Tags("Tareas de bodega")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-tasks/{warehouse_task_id}/resume")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ResumeAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid warehouse_task_id,
        CancellationToken cancellationToken)
        => SendAsync(new ResumeWarehouseTaskCommand
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            WarehouseTaskId = warehouse_task_id
        }, cancellationToken);

    private async Task<IActionResult> SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : ERP.Core.Warehouse.Api.Domain.Entities.Bases.BaseRequest
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        request.UserId = userId;
        var response = await mediator.Send(request, cancellationToken);
        return Ok(response);
    }
}
