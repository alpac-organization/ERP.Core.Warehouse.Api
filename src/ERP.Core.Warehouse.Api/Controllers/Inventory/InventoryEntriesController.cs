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
