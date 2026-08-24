using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class WarehouseController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Almacenes")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse")]
    [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> RegisterWarehouseAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromBody] RegisterWarehouseCommand payload,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        payload.CompanyId = company_id;
        payload.ModuleCode = module_code;
        payload.UserId = userId;

        var response = await _mediator.Send(payload, cancellationToken);

        return Created(string.Empty, response);
    }

    [Tags("Almacenes")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse")]
    [ProducesResponseType(typeof(PagedResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<WarehouseDto>> GetWarehouseAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromQuery] string? branch_code = null,
    [FromQuery] string? warehouse_code = null,
    [FromQuery] WarehouseType? warehouse_type = null,
    [FromQuery] bool? is_active = null,
    [FromQuery] int page_number = 1,
    [FromQuery] int page_size = 10,
    CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetWarehousesQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            BranchCode = branch_code,
            WarehouseCode = warehouse_code,
            WarehouseType = warehouse_type,
            IsActive = is_active,
            PageNumber = page_number,
            PageSize = page_size,
            UserId = userId,
        }, cancellationToken);
    }

    [Tags("Almacenes")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/subwarehouses")]
    [ProducesResponseType(typeof(PagedResponse<WarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<WarehouseDto>> GetSubWarehousesAsync(
                [FromRoute] Guid company_id,
                [FromRoute] string module_code,
                [FromRoute] Guid warehouse_id,
                [FromQuery] string? warehouse_code = null,
                [FromQuery] bool? is_active = null,
                [FromQuery] bool? is_owner = null,
                [FromQuery] string? search = null,
                [FromQuery] int page_number = 1,
                [FromQuery] int page_size = 10,
                CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        
        return await _mediator.Send(new GetSubWarehousesQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            ParentWarehouseId = warehouse_id,
            WarehouseCode = warehouse_code,
            IsActive = is_active,
            IsOwner = is_owner,
            Search = search,
            PageNumber = page_number,
            PageSize = page_size,
            UserId = Guid.Parse(userIdStr ?? ""),
        }, cancellationToken);
    }
}