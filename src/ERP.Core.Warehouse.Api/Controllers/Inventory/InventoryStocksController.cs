using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Inventory
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class InventoryStocksController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Inventario - Existencias")]
        [HttpGet("companies/{company_id}/modules/{module_code}/inventory-stock")]
        [ProducesResponseType(typeof(PagedResponseStock<InventoryStockDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponseStock<InventoryStockDto>> GetInventoryStockAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] string? product_code = null,
            [FromQuery] string? product_name = null,
            [FromQuery] Guid? category_id = null,
            [FromQuery] bool? is_fuel = null,
            [FromQuery] bool? is_material = null,
            [FromQuery] Guid? cost_center_id = null,
            [FromQuery] int page_size = 10,
            [FromQuery] int page_number = 1
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            
            //TODO: Falta agregar el rango de fechas...

            return await _mediator.Send(new GetInventoryStocksQuery()
            {
                CompanyId    = company_id,
                ModuleCode   = module_code,
                CategoryId   = category_id,
                IsFuel       = is_fuel,
                IsMaterial   = is_material,
                ProductCode  = product_code,
                ProductName  = product_name,
                CostCenterId = cost_center_id,
                UserId       = Guid.Parse(userIdStr ?? ""),
                PageSize     = page_size,
                PageNumber   = page_number
            });
        }

        [Tags("Inventario - Existencias")]
        [HttpPatch("companies/{company_id}/modules/{module_code}/inventory-stock/{inventory_stock_id}")]
        [ProducesResponseType(typeof(OkResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<OkResult> PatchInventoryStockAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid inventory_stock_id, [FromBody] PatchInventoryStockCommand? payload)
        {
            payload ??= new PatchInventoryStockCommand();
            payload.UserId = CurrentUserId;
            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.InventoryStockId = inventory_stock_id;

            await _mediator.Send(payload);
            
            return Ok();
        }
    }
}
