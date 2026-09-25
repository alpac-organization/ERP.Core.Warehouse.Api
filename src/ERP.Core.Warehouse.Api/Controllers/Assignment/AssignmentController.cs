using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Controllers.Assignment;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1")]
public class AssignmentController(IMediator mediator) : ApiControllerBase
{
    [Tags("Asignamiento")]
    [HttpGet("companies/{company_id}/modules/{module_code}/assignment")]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<object>>> GetAssignmentAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] int page_size = 10,
        [FromQuery] int page_number = 1,
        CancellationToken cancellationToken = default)
    {
        _ = mediator;
        _ = company_id;
        _ = module_code;
        _ = page_size;
        _ = page_number;
        _ = cancellationToken;

        await Task.CompletedTask;

        return Ok(new PagedResponse<object>([], page_number, page_size));
    }
}