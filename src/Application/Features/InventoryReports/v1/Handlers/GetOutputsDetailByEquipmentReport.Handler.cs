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
/// Genera el reporte de Salidas a Detalle por Equipo: listado plano ordenado por equipo y categoria.
/// </summary>
public class GetOutputsDetailByEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : OutputReportHandlerBase<GetOutputsDetailByEquipmentReportQuery, OutputDetailByEquipmentReportDto>(unitOfWork, errorManager)
{
    public override async Task<OutputDetailByEquipmentReportDto> Handle(GetOutputsDetailByEquipmentReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var validationError = ValidateAndResolveBranch(request, false, access, out var branchId);
        if (validationError != null)
            return validationError;

        var movements = await LoadKardexAsync(request, branchId, ct);
        var ordered = OrderSources(BuildSources(movements, request.CategoryId, request.ProductIds));
        var items = mapper.Map<List<OutputDetailByEquipmentItemDto>>(ordered);

        var generatedBy = await GetGeneratedByAsync(request.UserId, ct);

        return new OutputDetailByEquipmentReportDto
        {
            GeneratedAt = NicaraguaTime.Now(),
            GeneratedBy = generatedBy,
            From = request.From!.Value,
            To = request.To!.Value,
            Items = items,
            TotalQuantity = items.Sum(i => i.Quantity),
            TotalCost = items.Sum(i => i.Cost),
            TotalTotalCost = items.Sum(i => i.TotalCost)
        };
    }

    private static List<OutputReportSource> OrderSources(List<OutputReportSource> sources)
    {
        return [.. sources
            .OrderBy(GetEquipmentCode)
            .ThenBy(s => GetCategoryNameOrDefault(s))
            .ThenBy(GetProductNameOrDefault)
            .ThenBy(GetOutputDate)
            .ThenBy(GetOutputCode)];
    }
}
