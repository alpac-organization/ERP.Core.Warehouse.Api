using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.Inventory
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class InventoryEntriesController(IMediator mediator) : ApiControllerBase
    {
        [Tags("Inventario - Entradas")]
        [HttpGet("companies/{company_id}/modules/{module_code}/inventory-entries")]
        [ProducesResponseType(typeof(PagedResponse<InventoryEntryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<InventoryEntryDto>> GetInventoryEntriesAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] Guid? branch_id = null,
            [FromQuery] EntryStatus? entry_status = null,
            [FromQuery] string? entry_code = null,
            [FromQuery] string? purchase_request_code = null,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await mediator.Send(new GetInventoryEntriesQuery
            {
                BranchId    = branch_id,
                CompanyId   = company_id,
                ModuleCode  = module_code,
                UserId      = Guid.Parse(userIdStr ?? ""),
                EntryStatus = entry_status,
                EntryCode   = entry_code,
                PageNumber  = page_number,
                PageSize    = page_size,
                PurchaseRequestCode = purchase_request_code,
            });
        }

        [Tags("Inventario - Entradas")]
        [HttpGet("companies/{company_id}/modules/{module_code}/inventory-entries/{inventory_entry_id}/details")]
        [ProducesResponseType(typeof(InventoryEntryDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<InventoryEntryDetailDto> GetInventoryEntryDetailAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid inventory_entry_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await mediator.Send(new GetInventoryEntryDetailsQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                InventoryEntryId = inventory_entry_id,
            });
        }

        [Tags("Inventario - Entradas")]
        [HttpPost("companies/{company_id}/modules/{module_code}/inventory-entries")]
        [ProducesResponseType(typeof(PagedResponse<InventoryEntryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<InventoryEntryDto>> RegisteInventoryEntryAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] Guid? branch_id = null,
            [FromQuery] EntryStatus? entry_status = null,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await mediator.Send(new GetInventoryEntriesQuery
            {
                BranchId    = branch_id,
                CompanyId   = company_id,
                ModuleCode  = module_code,
                UserId      = Guid.Parse(userIdStr ?? ""),
                EntryStatus = entry_status,
                PageNumber  = page_number,
                PageSize    = page_size
            });
        }
    }
}
