using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Domain.Entities.Exceptions;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Unloading;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class UnloadingController(IMediator mediator) : ApiControllerBase
{
    #region Issue 1 - Cola de asignaciones
    [Tags("Descarga")]
    [HttpGet("companies/{company_id}/modules/{module_code}/unloading/assignment-queue")]
    [ProducesResponseType(typeof(GetAssignmentQueueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetAssignmentQueueDto> GetAssignmentQueueAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? service_order_code,
        [FromQuery] string? ducat_number,
        [FromQuery] string? warehouse_name,
        [FromQuery] UnloadingStatus? unloading_status,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            userId = Guid.Empty;
        }

        return await mediator.Send(new GetAssignmentQueueQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            ServiceOrderCode = service_order_code,
            DucatNumber = ducat_number,
            WarehouseName = warehouse_name,
            UnloadingStatus = unloading_status,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }
    #endregion

    #region Issue 2 - Detalle de asignación
    [Tags("Descarga")]
    [HttpGet("companies/{company_id}/modules/{module_code}/unloading/assignment-queue/{assignment_id}")]
    [ProducesResponseType(typeof(UnloadingAssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<UnloadingAssignmentDetailDto> GetUnloadingAssignmentAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid assignment_id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            userId = Guid.Empty;
        }

        return await mediator.Send(new GetUnloadingAssignmentDetailQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            AssignmentId = assignment_id
        }, cancellationToken);
    }
    #endregion

    #region Detalle de descarga
    [Tags("Descarga")]
    [HttpGet("companies/{company_id}/modules/{module_code}/unloading/assignment-queue/{assignment_id}/detail")]
    [ProducesResponseType(typeof(UnloadingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<UnloadingDetailDto> GetUnloadingDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid assignment_id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
            userId = Guid.Empty;

        return await mediator.Send(new GetUnloadingDetailQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            AssignmentId = assignment_id
        }, cancellationToken);
    }
    #endregion

    #region Issue 3 - Iniciar descarga
    [Tags("Descarga")]
    [HttpPost("companies/{company_id}/modules/{module_code}/unloading/assignment-queue/{assignment_id}/start")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> StartUnloadingAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid assignment_id,
        [FromBody] StartUnloadingCommand body,
        CancellationToken cancellationToken)
        => SendAsync(new StartUnloadingCommand
        {
            AssignmentId = assignment_id,
            StartDate = body.StartDate,
            StartTime = body.StartTime,
            MerchandiseType = body.MerchandiseType,
            Pallets = body.Pallets,
            Supplies = body.Supplies,
            CompanyId = company_id,
            ModuleCode = module_code
        }, cancellationToken);
    #endregion

    private async Task<IActionResult> SendAsync<TRequest>(TRequest request,
        CancellationToken ct, bool created = false)
        where TRequest : BaseRequest
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        request.UserId = userId;
        try
        {
            var response = await mediator.Send(request, ct);
            return created ? Created(string.Empty, response) : Ok(response);
        }
        catch (CoreException ex)
        {
            return BadRequest(ex.ErrorData);
        }
    }
}