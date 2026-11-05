using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.ReviewFuelRequest.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.ReviewFuelRequest;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
[Tags("Revisión de solicitudes de Combustible")]
public class ReviewFuelRequestsController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("companies/{company_id}/modules/{module_code}/inventory/fuel-requests/{fuel_request_id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<NoContentResult> ApproveFuelRequestAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid fuel_request_id,
        [FromBody] ApproveFuelRequestCommand command)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        command.CompanyId = company_id;
        command.ModuleCode = module_code;
        command.UserId = Guid.Parse(userIdStr ?? "");
        command.FuelRequestId = fuel_request_id;

        await mediator.Send(command);

        return NoContent();
    }

    [HttpPost("companies/{company_id}/modules/{module_code}/inventory/fuel-requests/{fuel_request_id}/decline")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<NoContentResult> DeclineFuelRequestAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid fuel_request_id,
        [FromBody] DeclineFuelRequestCommand command)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        command.CompanyId = company_id;
        command.ModuleCode = module_code;
        command.UserId = Guid.Parse(userIdStr ?? "");
        command.FuelRequestId = fuel_request_id;

        await mediator.Send(command);

        return NoContent();
    }
}