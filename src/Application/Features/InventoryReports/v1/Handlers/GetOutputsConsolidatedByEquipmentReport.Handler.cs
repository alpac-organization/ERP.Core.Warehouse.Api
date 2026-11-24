using AutoMapper;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

using static ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons.OutputReportHelper;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Salidas Consolidado por Equipo: categorias con items consolidados por equipo, producto, centro de costo y cliente.
/// </summary>
public class GetOutputsConsolidatedByEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : OutputReportHandlerBase<GetOutputsConsolidatedByEquipmentReportQuery, OutputConsolidatedByEquipmentReportDto>(unitOfWork, errorManager)
{
    public override async Task<OutputConsolidatedByEquipmentReportDto> Handle(GetOutputsConsolidatedByEquipmentReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var validationError = ValidateAndResolveBranch(request, false, access, out var branchId);
        if (validationError != null)
            return validationError;

        var movements = await LoadKardexAsync(request, branchId, ct);
        var ordered = OrderSources(BuildSources(movements, request.CategoryId, request.ProductIds));

        var generatedBy = await GetGeneratedByAsync(request.UserId, ct);

        return BuildReport(ordered, request, generatedBy);
    }

    private static List<OutputReportSource> OrderSources(List<OutputReportSource> sources)
    {
        return [.. sources
            .OrderBy(s => GetCategoryNameOrDefault(s))
            .ThenBy(GetProductNameOrDefault)];
    }

    private OutputConsolidatedByEquipmentReportDto BuildReport(
        List<OutputReportSource> sources,
        GetOutputsConsolidatedByEquipmentReportQuery request,
        string? generatedBy)
    {
        return new OutputConsolidatedByEquipmentReportDto
        {
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = generatedBy,
            From = request.From!.Value,
            To = request.To!.Value,
            Categories = [.. sources
                .GroupBy(GetCategoryId)
                .Select(categoryGroup =>
                {
                    List<OutputConsolidatedItemDto> items = [.. categoryGroup
                        .GroupBy(s => new
                        {
                            EquipmentId = GetEquipmentId(s),
                            ProductId = GetProductId(s),
                            s.Output.CostCenterId,
                            CustomerId = s.IsFuel ? s.Output.FuelRequest?.CustomerId : s.Output.SupplyRequest?.CustomerId
                        })
                        .Select(group =>
                        {
                            var item = mapper.Map<OutputConsolidatedItemDto>(group.First());
                            item.Quantity = group.Sum(GetQuantity);
                            item.TotalCost = group.Sum(GetTotalCost);
                            return item;
                        })
                        .OrderBy(i => i.Supply)];

                    return new OutputConsolidatedCategoryDto
                    {
                        CategoryName = GetCategoryNameOrDefault(categoryGroup.First()),
                        Items = items,
                        TotalQuantity = items.Sum(i => i.Quantity),
                        TotalCost = items.Sum(i => i.TotalCost)
                    };
                })]
        };
    }
}
