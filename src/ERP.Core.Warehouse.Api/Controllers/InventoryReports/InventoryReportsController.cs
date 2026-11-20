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

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/physical-count")]
    [ProducesResponseType(typeof(List<PhysicalCountReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<PhysicalCountReportDto>> GetPhysicalCountReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] Guid? area_id = null,
        [FromQuery] Guid? cost_center_id = null,
        [FromQuery] Guid? category_id = null,
        [FromQuery] string? product_code = null,
        [FromQuery] string? product_name = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetPhysicalCountReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id,
            AreaId = area_id,
            CostCenterId = cost_center_id,
            CategoryId = category_id,
            ProductCode = product_code,
            ProductName = product_name
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/inventory-movement")]
    [ProducesResponseType(typeof(InventoryMovementReportDto), StatusCodes.Status200OK)]
    public async Task<InventoryMovementReportDto> GetInventoryMovementReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] Guid? cost_center_id = null,
        [FromQuery] Guid? product_id = null,
        [FromQuery] string? product_code = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        return await mediator.Send(new GetInventoryMovementReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id,
            CostCenterId = cost_center_id,
            ProductId = product_id,
            ProductCode = product_code
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/inventory-turnover")]
    [ProducesResponseType(typeof(List<InventoryTurnoverReportDto>), StatusCodes.Status200OK)]
    public async Task<List<InventoryTurnoverReportDto>> GetInventoryTurnoverReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] Guid? cost_center_id = null,
        [FromQuery] Guid? product_id = null,
        [FromQuery] string? product_code = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        return await mediator.Send(new GetInventoryTurnoverReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id,
            CostCenterId = cost_center_id,
            ProductId = product_id,
            ProductCode = product_code
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/outputs-line-equipment")]
    [ProducesResponseType(typeof(List<OutputLineEquipmentReportDto>), StatusCodes.Status200OK)]
    public async Task<List<OutputLineEquipmentReportDto>> GetOutputsLineEquipmentReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] Guid? category_id = null,
        [FromQuery] Guid? equipment_id = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        return await mediator.Send(new GetOutputsLineEquipmentReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id,
            CategoryId = category_id,
            EquipmentId = equipment_id
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/outputs-detail-by-equipment")]
    [ProducesResponseType(typeof(List<OutputDetailByEquipmentReportDto>), StatusCodes.Status200OK)]
    public async Task<List<OutputDetailByEquipmentReportDto>> GetOutputsDetailByEquipmentReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        return await mediator.Send(new GetOutputsDetailByEquipmentReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/outputs-consolidated-by-equipment")]
    [ProducesResponseType(typeof(List<OutputConsolidatedByEquipmentReportDto>), StatusCodes.Status200OK)]
    public async Task<List<OutputConsolidatedByEquipmentReportDto>> GetOutputsConsolidatedByEquipmentReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery(Name = "equipment_ids")] List<Guid>? equipment_ids = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        return await mediator.Send(new GetOutputsConsolidatedByEquipmentReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id,
            EquipmentIds = equipment_ids
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/period-inventory")]
    [ProducesResponseType(typeof(List<PeriodInventoryReportDto>), StatusCodes.Status200OK)]
    public async Task<List<PeriodInventoryReportDto>> GetPeriodInventoryReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] Guid? cost_center_id = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        return await mediator.Send(new GetPeriodInventoryReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id,
            CostCenterId = cost_center_id
        });
    }
}