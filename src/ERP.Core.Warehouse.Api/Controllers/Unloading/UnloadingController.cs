using MediatR;
using Microsoft.AspNetCore.Mvc;
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
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await mediator.Send(new GetAssignmentQueueQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }
    #endregion
}