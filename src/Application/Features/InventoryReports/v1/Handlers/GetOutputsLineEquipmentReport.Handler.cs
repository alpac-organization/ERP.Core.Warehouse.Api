using AutoMapper;

using ERP.Core.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

using static ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons.OutputReportHelper;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Salidas Linea-Equipo: detalle de salidas por producto, agrupado por categoria y equipo.
/// </summary>
public class GetOutputsLineEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : OutputReportHandlerBase<GetOutputsLineEquipmentReportQuery, OutputLineEquipmentReportDto>(unitOfWork, errorManager)
{
    public override async Task<OutputLineEquipmentReportDto> Handle(GetOutputsLineEquipmentReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var validationError = ValidateAndResolveBranch(request, true, access, out var branchId);
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
            .ThenBy(GetEquipmentCode)
            .ThenBy(GetProductNameOrDefault)
            .ThenBy(s => s.Output.Branch != null ? s.Output.Branch.BranchCode : "")];
    }

    private OutputLineEquipmentReportDto BuildReport(
        List<OutputReportSource> sources,
        GetOutputsLineEquipmentReportQuery request,
        string? generatedBy)
    {
        return new OutputLineEquipmentReportDto
        {
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = generatedBy,
            From = request.From!.Value,
            To = request.To!.Value,
            Categories = [.. sources
                .GroupBy(GetCategoryId)
                .Select(categoryGroup => new OutputLineEquipmentCategoryDto
                {
                    CategoryName = GetCategoryNameOrDefault(categoryGroup.First()),
                    Equipments = [.. categoryGroup
                        .GroupBy(GetEquipmentId)
                        .Select(equipmentGroup =>
                        {
                            var firstSource = equipmentGroup.First();
                            var equipment = firstSource.IsFuel
                                ? firstSource.Output.FuelRequest?.Equipment
                                : firstSource.Output.SupplyRequest?.Equipment;

                            return new OutputLineEquipmentEquipmentDto
                            {
                                EquipmentInformation = equipment != null
                                    ? mapper.Map<EquipmentInformation>(equipment)
                                    : new EquipmentInformation(),
                                Products = [.. equipmentGroup
                                    .GroupBy(GetProductId)
                                    .Select(productGroup =>
                                    {
                                        var items = productGroup.Select(s => mapper.Map<OutputLineEquipmentItemDto>(s)).ToList();

                                        return new OutputLineEquipmentProductDto
                                        {
                                            StockCode = GetStockCode(productGroup.First()),
                                            Supply = GetProductNameOrDefault(productGroup.First()),
                                            Items = items,
                                            TotalQuantity = items.Sum(i => i.Quantity),
                                            TotalCost = items.Sum(i => i.Cost),
                                            TotalTotalCost = items.Sum(i => i.TotalCost)
                                        };
                                    })]
                            };
                        })]
                })]
        };
    }
}
