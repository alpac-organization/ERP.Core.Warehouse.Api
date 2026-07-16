using MediatR;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using Superpower;

namespace ERP.Core.Warehouse.Api.Controllers.Reception;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ReceptionEntranceController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Control de Acceso")]
    [HttpPost("companies/{company_id}/modules/{module_code}/reception-entrances")]
    [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> CreateReceptionEntranceAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] CreateReceptionEntranceDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToCommand(
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Created(string.Empty, response);
    }
}