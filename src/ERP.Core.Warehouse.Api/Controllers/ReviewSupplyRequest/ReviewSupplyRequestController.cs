using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.ReviewSupplyRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.ReviewsupplyRequest;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
[Tags("Revisión de solicitudes Operacionales de Inventario")]
public class SupplyRequestsController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("companies/{company_id}/modules/{module_code}/inventory/supply-requests/{supply_request_id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<NoContentResult> ApproveSupplyRequestAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid supply_request_id,
        [FromBody] ApproveSupplyRequestCommand command)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        command.CompanyId = company_id;
        command.ModuleCode = module_code;
        command.UserId = Guid.Parse(userIdStr ?? "");
        command.SupplyRequestId = supply_request_id;

        await mediator.Send(command);

        return NoContent();
    }

}