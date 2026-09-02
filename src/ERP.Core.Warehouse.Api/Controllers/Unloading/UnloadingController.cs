using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

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
}