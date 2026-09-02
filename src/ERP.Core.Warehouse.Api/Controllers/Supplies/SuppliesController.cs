using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.Supplies;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class SuppliesController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Catálogos")]
    [HttpGet("companies/{company_id}/modules/{module_code}/supplies")]
    [ProducesResponseType(typeof(List<SupplyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<SupplyDto>> GetSuppliesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            userId = Guid.Empty;
        }

        return await _mediator.Send(new GetSuppliesQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId
        }, cancellationToken);
    }

    [Tags("Catálogos")]
    [HttpPost("companies/{company_id}/modules/{module_code}/supplies")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> RegisterSupplyAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] RegisterSupplyDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            userId = Guid.Empty;
        }

        var command = dto.ToCommand(
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }
}
