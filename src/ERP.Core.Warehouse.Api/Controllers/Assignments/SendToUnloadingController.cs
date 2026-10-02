using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Assignments;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
[Tags("Enviar a Bodega")]
public class SendToUnloaadingController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/send-to-unloading")]
    [ProducesResponseType(typeof(IActionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendToUnloadingAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid operational_order_id,
        [FromRoute] Guid assignment_id)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        var command = new SendToUnloadingCommand
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            OperationalOrderId = operational_order_id,
            AssignmentId = assignment_id,
            UserId = Guid.Parse(userIdStr ?? "")
        };

        await mediator.Send(command);

        return Ok();
    }
}