using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.ShippingCompanies;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ShippingCompaniesController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Catálogos")]
    [HttpGet("companies/{company_id}/modules/{module_code}/shipping-companies")]
    [ProducesResponseType(typeof(List<ShippingCompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<ShippingCompanyDto>> GetShippingCompaniesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetShippingCompaniesQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId
        }, cancellationToken);
    }
}