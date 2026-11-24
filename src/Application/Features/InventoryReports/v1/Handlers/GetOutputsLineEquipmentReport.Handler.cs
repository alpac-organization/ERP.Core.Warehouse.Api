using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Salidas Linea-Equipo: detalle de salidas por producto, agrupado por categoria y equipo.
/// </summary>
public class GetOutputsLineEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetOutputsLineEquipmentReportQuery, OutputLineEquipmentReportDto>(unitOfWork, errorManager)
{
    public override async Task<OutputLineEquipmentReportDto> Handle(GetOutputsLineEquipmentReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var validationError = ValidateAndResolveBranch(request, access, out var branchId);
        if (validationError != null)
            return validationError;

        var outputs = await LoadOutputsAsync(request, branchId, ct);
        var ordered = OrderSources(BuildSources(outputs, request));

        var generatedBy = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.Fullname)
            .FirstOrDefaultAsync(ct);

        return BuildReport(ordered, request, generatedBy);
    }

    private OutputLineEquipmentReportDto? ValidateAndResolveBranch(
        GetOutputsLineEquipmentReportQuery request,
        AccessValidationResult<OutputLineEquipmentReportDto> access,
        out Guid branchId)
    {
        branchId = Guid.Empty;

        if (!request.From.HasValue || !request.To.HasValue)
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");

        if (request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("El periodo no es valido.", "ERP:010");

        if (request.CategoryId is not { Count: > 0 })
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("Debe enviar al menos una categoria.", "ERP:009");

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

        if (!isAdministrator && access.Profile == null)
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("Perfil no encontrado.", "ERP:011");

        if (isAdministrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("Debe especificar la sucursal.", "ERP:008");

        branchId = isAdministrator ? request.BranchId!.Value : access.Profile.BranchId;
        return null;
    }

    private async Task<List<ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs>> LoadOutputsAsync(GetOutputsLineEquipmentReportQuery request, Guid branchId, CancellationToken ct)
    {
        var query = _unitOfWork.InventoryOutputs.Entities
            .AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.CostCenter)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s!.SupplyRequestItems)
                    .ThenInclude(i => i.ProductStock)
                        .ThenInclude(ps => ps!.Product)
                            .ThenInclude(p => p!.Category)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s!.Equipment)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s!.OperationalOrder)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s!.Customer)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f!.ProductStock)
                    .ThenInclude(ps => ps!.Product)
                        .ThenInclude(p => p!.Category)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f!.Equipment)
            .Include(o => o.FuelRequest)
                .ThenInclude(f => f!.Customer)
            .Where(o => o.DeletedAt == null)
            .Where(o => o.BranchId == branchId)
            .Where(o => (o.DispatchedAt ?? o.CreatedAt) >= request.From!.Value)
            .Where(o => (o.DispatchedAt ?? o.CreatedAt) <= request.To!.Value.Date.AddDays(1).AddTicks(-1));

        if (request.EquipmentIds is { Count: > 0 })
        {
            query = query.Where(o => (o.SupplyRequest != null && request.EquipmentIds.Contains(o.SupplyRequest.EquipmentId ?? Guid.Empty)) ||
                                    (o.FuelRequest != null && request.EquipmentIds.Contains(o.FuelRequest.EquipmentId)));
        }

        return await query.ToListAsync(ct);
    }

    private static List<OutputLineEquipmentSource> BuildSources(List<ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs> outputs, GetOutputsLineEquipmentReportQuery request)
    {
        var sources = new List<OutputLineEquipmentSource>();

        foreach (var output in outputs)
        {
            if (output.SupplyRequest?.Equipment != null)
            {
                foreach (var item in output.SupplyRequest.SupplyRequestItems)
                {
                    var product = item.ProductStock?.Product;
                    if (!MatchesFilters(product?.CategoryId, item.ProductStock?.ProductId, request))
                        continue;

                    sources.Add(new OutputLineEquipmentSource
                    {
                        Output = output,
                        Item = item,
                        IsFuel = false
                    });
                }
            }

            if (output.FuelRequest?.Equipment != null)
            {
                var product = output.FuelRequest.ProductStock?.Product;
                if (!MatchesFilters(product?.CategoryId, output.FuelRequest.ProductStock?.ProductId, request))
                    continue;

                sources.Add(new OutputLineEquipmentSource
                {
                    Output = output,
                    Item = null,
                    IsFuel = true
                });
            }
        }

        return sources;
    }

    private static bool MatchesFilters(Guid? categoryId, Guid? productId, GetOutputsLineEquipmentReportQuery request)
    {
        if (request.CategoryId is { Count: > 0 } && !request.CategoryId.Contains(categoryId ?? Guid.Empty))
            return false;

        if (request.ProductIds is { Count: > 0 } && !request.ProductIds.Contains(productId ?? Guid.Empty))
            return false;

        return true;
    }

    private static List<OutputLineEquipmentSource> OrderSources(List<OutputLineEquipmentSource> sources)
    {
        return [.. sources
            .OrderBy(GetCategoryName)
            .ThenBy(GetEquipmentCode)
            .ThenBy(GetProductName)
            .ThenBy(s => s.Output.Branch != null ? s.Output.Branch.BranchCode : "")];
    }

    private OutputLineEquipmentReportDto BuildReport(
        List<OutputLineEquipmentSource> sources,
        GetOutputsLineEquipmentReportQuery request,
        string? generatedBy)
    {
        return new OutputLineEquipmentReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = generatedBy,
            From = request.From!.Value,
            To = request.To!.Value,
            Categories = [.. sources
                .GroupBy(GetCategoryId)
                .Select(categoryGroup => new OutputLineEquipmentCategoryDto
                {
                    CategoryName = GetCategoryName(categoryGroup.First()),
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
                                            Supply = GetProductName(productGroup.First()),
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

    private static Guid GetCategoryId(OutputLineEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.ProductStock?.Product?.CategoryId ?? Guid.Empty
        : source.Item?.ProductStock?.Product?.CategoryId ?? Guid.Empty;

    private static string GetCategoryName(OutputLineEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.ProductStock?.Product?.Category?.Name ?? "Sin categoria"
        : source.Item?.ProductStock?.Product?.Category?.Name ?? "Sin categoria";

    private static Guid GetEquipmentId(OutputLineEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.EquipmentId ?? Guid.Empty
        : source.Output.SupplyRequest?.EquipmentId ?? Guid.Empty;

    private static string GetEquipmentCode(OutputLineEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.Equipment?.Code ?? ""
        : source.Output.SupplyRequest?.Equipment?.Code ?? "";

    private static Guid GetProductId(OutputLineEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.ProductStock?.ProductId ?? Guid.Empty
        : source.Item?.ProductStock?.ProductId ?? Guid.Empty;

    private static string GetProductName(OutputLineEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.ProductStock?.Product?.ProductName ?? ""
        : source.Item?.ProductStock?.Product?.ProductName ?? "";

    private static string? GetStockCode(OutputLineEquipmentSource source)
    {
        var product = source.IsFuel
            ? source.Output.FuelRequest?.ProductStock?.Product
            : source.Item?.ProductStock?.Product;

        return product?.Code;
    }
}
