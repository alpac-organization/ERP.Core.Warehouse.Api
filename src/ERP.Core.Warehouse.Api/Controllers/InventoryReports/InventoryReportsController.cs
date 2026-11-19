using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Controllers.InventoryReports;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
[Tags("Reportes de Inventario")]
public class InventoryReportsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("companies/{company_id}/modules/{module_code}/reports/stock-daily")]
    [ProducesResponseType(typeof(StockDailyReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<StockDailyReportDto> GetStockDailyReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] Guid? area_id = null,
        [FromQuery] Guid? cost_center_id = null,
        [FromQuery] Guid? category_id = null,
        [FromQuery] string? product_code = null,
        [FromQuery] string? product_name = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetStockDailyReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            BranchId = branch_id,
            AreaId = area_id,
            CostCenterId = cost_center_id,
            CategoryId = category_id,
            ProductCode = product_code,
            ProductName = product_name
        });
    }
}