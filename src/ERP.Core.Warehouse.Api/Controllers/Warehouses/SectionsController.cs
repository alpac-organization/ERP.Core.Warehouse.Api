using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

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

    [Tags("Secciones")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections")]
    [ProducesResponseType(typeof(PagedResponse<SectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<SectionDto>> GetSectionsAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid warehouse_id,
    [FromQuery] string? section_code = null,
    [FromQuery] SectionType? section_type = null,
    [FromQuery] SectionStorageType? section_storage_type = null,
    [FromQuery] bool? is_active = null,
    [FromQuery] int page_number = 1,
    [FromQuery] int page_size = 10,
    CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetSectionsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            WarehouseId = warehouse_id,
            SectionType = section_type,
            SectionStorageType = section_storage_type,
            IsActive = is_active,
            SectionCode = section_code,
            PageNumber = page_number,
            PageSize = page_size,
        }, cancellationToken);
    }

    [Tags("Secciones")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections/{section_id}")]
    [ProducesResponseType(typeof(SectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<SectionDto> GetSectionByIdAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid warehouse_id,
    [FromRoute] Guid section_id,
    CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetSectionByIdQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            WarehouseId = warehouse_id,
            SectionId = section_id,
        }, cancellationToken);
    }

}
