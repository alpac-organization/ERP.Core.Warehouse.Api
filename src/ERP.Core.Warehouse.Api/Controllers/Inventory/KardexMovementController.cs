using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.Inventory
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class KardexMovementController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Inventario - Movimientos Kardex")]
        [HttpGet("companies/{company_id}/modules/{module_code}/kardex-movement")]
        [ProducesResponseType(typeof(PagedResponse<KardexDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<KardexDto>> GetInventoryStockAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] string? product_code     = null,
            [FromQuery] Guid? category_id        = null,
            [FromQuery] KardexType? kardex_type  = null,
            [FromQuery] KardexMovementType? movement_type = null,
            [FromQuery] int page_size = 10,
            [FromQuery] int page_number = 1
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            
            return await _mediator.Send(new GetKardexMovementQuery()
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                CategoryId = category_id,
                ProductCode = product_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                
                KardexType = kardex_type,
                KardexMovementType = movement_type,
                PageSize = page_size,
                PageNumber = page_number
            });
        }
    }
}
