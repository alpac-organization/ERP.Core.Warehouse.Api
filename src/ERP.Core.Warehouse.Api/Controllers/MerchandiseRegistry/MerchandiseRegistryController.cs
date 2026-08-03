using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.MerchandiseRegistry;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class MerchandiseRegistryController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Registro de Mercadería")]
    [HttpGet("companies/{company_id}/modules/{module_code}/merchandise-registry")]
    [ProducesResponseType(typeof(GetMerchandiseRegistryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetMerchandiseRegistryDto> GetMerchandiseRegistryAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetMerchandiseRegistryQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }

    [Tags("Registro de Mercadería")]
    [HttpGet("companies/{company_id}/modules/{module_code}/merchandise-registry/{reception_id}")]
    [ProducesResponseType(typeof(GetMerchandiseRegistryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetMerchandiseRegistryDetailDto> GetMerchandiseRegistryDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetMerchandiseRegistryDetailsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            ReceptionId = reception_id,
            UserId = userId
        }, cancellationToken);
    }

    [Tags("Registro de Mercadería")]
    [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/ducat-registry")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CreateDucatRegistryAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        [FromBody] CreateDucatRegistryDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = _mapper.Map<CreateDucatRegistryCommand>(dto);
        command.ReceptionId = reception_id;
        command.UserId = userId;
        command.CompanyId = company_id;
        command.ModuleCode = module_code;

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }
}