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
    [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> RegisterSectionAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid warehouse_id, [FromBody] RegisterSectionCommand payload, CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        payload.CompanyId = company_id;
        payload.ModuleCode = module_code;
        payload.UserId = Guid.Parse(userIdStr ?? "");
        payload.WarehouseId = warehouse_id;

        await _mediator.Send(payload, cancellationToken);

        return Created();
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
    [FromQuery] bool? is_active = null,
    [FromQuery] int page_number = 1,
    [FromQuery] int page_size = 10,
    CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetSectionsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            WarehouseId = warehouse_id,
            SectionType = section_type,
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

        return await _mediator.Send(new GetSectionByIdQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            WarehouseId = warehouse_id,
            SectionId = section_id,
        }, cancellationToken);
    }

    [Tags("Detalle de Seccion")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections/{section_id}/details")]
    [ProducesResponseType(typeof(SectionCapacitiesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<SectionCapacitiesDto> GetSectionDetailsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid warehouse_id, [FromRoute] Guid section_id)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetSectionCapacitiesQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            SectionId = section_id
        });
    }

    [Tags("Actualizacion de Seccion")]
    [HttpPatch("companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections/{section_id}")]
    [ProducesResponseType(typeof(SectionCapacitiesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]

    public async Task<SectionCapacitiesDto> UpdateSectionAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid warehouse_id, [FromRoute] Guid section_id)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await _mediator.Send(new GetSectionCapacitiesQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            SectionId = section_id
        });
    }
}
