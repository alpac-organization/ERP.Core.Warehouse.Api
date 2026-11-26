using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands;

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
        [FromQuery] List<Guid>? branch_ids = null,
        [FromQuery] Guid? area_id = null,
        [FromQuery] List<Guid>? area_ids = null,
        [FromQuery] Guid? cost_center_id = null,
        [FromQuery] List<Guid>? cost_center_ids = null,
        [FromQuery] Guid? category_id = null,
        [FromQuery] List<Guid>? category_ids = null,
        [FromQuery] ProductUsageType? usage_type = null,
        [FromQuery] List<ProductUsageType>? usage_types = null,
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
            UsageType = usage_type,
            BranchIds = branch_ids,
            AreaIds = area_ids,
            CostCenterIds = cost_center_ids,
            CategoryIds = category_ids,
            UsageTypes = usage_types,
            ProductCode = product_code,
            ProductName = product_name
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/physical-count")]
    [ProducesResponseType(typeof(PhysicalCountReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PhysicalCountReportDto> GetPhysicalCountReportAsync(
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

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/inventory-period")]
    [ProducesResponseType(typeof(PeriodInventoryReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PeriodInventoryReportDto> GetPeriodInventoryReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
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

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/inventory-movements")]
    [ProducesResponseType(typeof(InventoryMovementReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
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

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/outputs-line-equipment")]
    [ProducesResponseType(typeof(OutputLineEquipmentReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OutputLineEquipmentReportDto> GetOutputsLineEquipmentReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] List<Guid>? category_ids = null,
        [FromQuery] List<Guid>? equipment_ids = null,
        [FromQuery] List<Guid>? product_ids = null)
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
            CategoryId = category_ids,
            EquipmentIds = equipment_ids,
            ProductIds = product_ids
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/outputs-detail-by-equipment")]
    [ProducesResponseType(typeof(OutputDetailByEquipmentReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OutputDetailByEquipmentReportDto> GetOutputsDetailByEquipmentReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] List<Guid>? category_ids = null,
        [FromQuery] List<Guid>? equipment_ids = null,
        [FromQuery] List<Guid>? product_ids = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        return await mediator.Send(new GetOutputsDetailByEquipmentReportQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            From = from,
            To = to,
            BranchId = branch_id,
            CategoryId = category_ids,
            EquipmentIds = equipment_ids,
            ProductIds = product_ids
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/outputs-consolidated-by-equipment")]
    [ProducesResponseType(typeof(OutputConsolidatedByEquipmentReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OutputConsolidatedByEquipmentReportDto> GetOutputsConsolidatedByEquipmentReportAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] Guid? branch_id = null,
        [FromQuery] List<Guid>? equipment_ids = null,
        [FromQuery] List<Guid>? category_ids = null,
        [FromQuery] List<Guid>? product_ids = null)
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
            EquipmentIds = equipment_ids,
            CategoryId = category_ids,
            ProductIds = product_ids
        });
    }

    [HttpPost("companies/{company_id}/modules/{module_code}/reports/physical-counts")]
    [ProducesResponseType(typeof(CreatePhysicalCountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatePhysicalCountResponse> CreatePhysicalCountAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] CreatePhysicalCountRequest? payload = null)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        var command = new CreatePhysicalCountCommand
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            Notes = payload?.Notes
        };

        return await mediator.Send(command);
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/physical-counts")]
    [ProducesResponseType(typeof(List<PhysicalCountListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<PhysicalCountListItemDto>> GetPhysicalCountsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetPhysicalCountsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? "")
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/physical-counts/{physical_count_id}/lines")]
    [ProducesResponseType(typeof(PhysicalCountLinesPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PhysicalCountLinesPageDto> GetPhysicalCountLinesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid physical_count_id,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 30)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetPhysicalCountLinesQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            PhysicalCountId = physical_count_id,
            PageNumber = page_number,
            PageSize = page_size
        });
    }

    [HttpPatch("companies/{company_id}/modules/{module_code}/reports/physical-counts/{physical_count_id}/lines")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePhysicalCountLinesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid physical_count_id,
        [FromBody] List<UpdatePhysicalCountLineItem> items)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        await mediator.Send(new UpdatePhysicalCountLinesCommand
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            PhysicalCountId = physical_count_id,
            Items = items
        });

        return Ok();
    }

    [HttpDelete("companies/{company_id}/modules/{module_code}/reports/physical-counts/{physical_count_id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<bool> CancelPhysicalCountAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid physical_count_id)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new CancelPhysicalCountCommand
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            PhysicalCountId = physical_count_id
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/reports/physical-counts/{physical_count_id}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<FileContentResult> GetPhysicalCountPdfAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid physical_count_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        var result = await mediator.Send(new GetPhysicalCountPdfQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = Guid.Parse(userIdStr ?? ""),
            PhysicalCountId = physical_count_id
        }, cancellationToken);

        return File(result.Bytes, "application/pdf", $"levantamiento-{result.CreatedAt:dd-MM-yyyy}.pdf");
    }
}
