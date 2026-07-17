using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using Superpower;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

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

        return Created(string.Empty, null);
    }

    [Tags("Control de Acceso")]
    [HttpGet("companies/{company_id}/modules/{module_code}/reception-entrances")]
    [ProducesResponseType(typeof(GetReceptionEntrancesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetReceptionEntrancesDto> GetReceptionEntrancesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? driver_name,
        [FromQuery] string? plate_number,
        [FromQuery] string? ducat_number,
        [FromQuery] DateTime? date,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetReceptionEntrancesQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            DriverName = driver_name,
            PlateNumber = plate_number,
            DucatNumber = ducat_number,
            Date = date,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }

}