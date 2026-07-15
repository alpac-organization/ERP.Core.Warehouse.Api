using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Reception;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ReceptionEntranceManaguaController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Control de Acceso")]
    [HttpPost("")]
    [ProducesResponseType(typeof(CreateReceptionEntranceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateReceptionEntranceResponse>> CreateReceptionEntranceAsync(
        // [FromRoute] ,
        [FromBody] CreateReceptionEntrancecommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}