using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.OperationalServices;

[HasToken]
[ApiVersion("1.0")]
[Route("/api/v1/")]
public class OperationalServicesController(IMediator mediator) : ApiControllerBase
{
    [Tags("Catálogo de servicios operativos")]
    [HttpPost("companies/{company_id}/modules/{module_code}/operational-services")]
    [ProducesResponseType(typeof(Unit), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterOperationalServicesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] RegisterOperationalServicesCommand command)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        command.CompanyId = company_id;
        command.ModuleCode = module_code;
        command.UserId = Guid.Parse(userIdStr ?? "");

        var result = await mediator.Send(command);
        return Ok(Unit.Value);
    }
}