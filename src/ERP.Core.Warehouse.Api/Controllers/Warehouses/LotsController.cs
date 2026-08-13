using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class LotsController(IMediator mediator) : ApiControllerBase
{
    [Tags("Tramos")]
    [HttpPost("companies/{company_id}/modules/{module_code}/sections/{section_id}/lots")]
    [ProducesResponseType(typeof(RegisterLotsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterLotsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid section_id,
        [FromBody] RegisterLotsDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var command = dto.ToCommand(section_id, userId, company_id, module_code);
        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }
}

