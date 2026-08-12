using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using MediatR;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Infrastructure.Attributes;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class WarehouseSectionsController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Secciones")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> RegisterSectionAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid warehouse_id,
    [FromBody] RegisterSectionCommand payload,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        payload.CompanyId = company_id;
        payload.ModuleCode = module_code;
        payload.UserId = userId;
        payload.WarehouseId = warehouse_id;

        var response = await _mediator.Send(payload, cancellationToken);

        return Created(string.Empty, response);
    }
}
