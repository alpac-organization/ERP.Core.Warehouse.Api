using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class RacksController(IMediator mediator) : ApiControllerBase
{
    [Tags("Racks")]
    [HttpPost("companies/{company_id}/modules/{module_code}/sections/{section_id}/racks")]
    [ProducesResponseType(typeof(RegisterRacksBulkResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterRacksAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid section_id,
        [FromBody] RegisterRacksBulkDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var command = dto.ToCommand(
            sectionId: section_id,
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Tags("Racks")]
    [HttpGet("companies/{company_id}/modules/{module_code}/sections/{section_id}/racks/summary")]
    [ProducesResponseType(typeof(RackSectionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRackSectionSummaryAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid section_id,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var query = new GetRackSectionSummaryQuery
        {
            SectionId = section_id,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await mediator.Send(query, cancellationToken);
        return Ok(response);
    }
}