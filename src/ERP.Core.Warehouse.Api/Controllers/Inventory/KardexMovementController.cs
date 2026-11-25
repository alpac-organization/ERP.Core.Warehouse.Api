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
        public async Task<PagedResponse<KardexDto>> GetInventoryStockAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromQuery] Guid? branch_id = null,
            [FromQuery] Guid? cost_center_id = null,
            [FromQuery] Guid? work_area_id = null,
            [FromQuery] string? product_code = null,
            [FromQuery] Guid? category_id = null,
            [FromQuery] KardexType? kardex_type = null,
            [FromQuery] KardexMovementType? movement_type = null,
            [FromQuery] DateTime? date_from = null,
            [FromQuery] DateTime? date_to = null,
            [FromQuery] int page_size = 10,
            [FromQuery] int page_number = 1
        )
        {
            return await _mediator.Send(new GetKardexMovementQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = CurrentUserId,

                BranchId = branch_id,
                CostCenterId = cost_center_id,
                WorkAreaId = work_area_id,
                CategoryId = category_id,
                ProductCode = product_code,
                KardexType = kardex_type,
                KardexMovementType = movement_type,
                DateFrom = date_from,
                DateTo = date_to,
                PageSize = page_size,
                PageNumber = page_number
            });
        }

        [Tags("Inventario - Movimientos Kardex")]
        [HttpGet("companies/{company_id}/modules/{module_code}/kardex-movement/{kardex_id}/details")]
        [ProducesResponseType(typeof(KardexDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<KardexDetailsDto> GetKardexMovementDetailsAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid kardex_id
        )
        {
            return await _mediator.Send(new GetKardexMovementDetailsQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = CurrentUserId,
                KardexId = kardex_id
            });
        }
    }
}
