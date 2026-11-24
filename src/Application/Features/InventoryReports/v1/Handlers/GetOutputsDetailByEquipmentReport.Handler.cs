using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Utils;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Salidas a Detalle por Equipo: listado plano ordenado por equipo y categoria.
/// </summary>
public class GetOutputsDetailByEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetOutputsDetailByEquipmentReportQuery, OutputDetailByEquipmentReportDto>(unitOfWork, errorManager)
{
    public override async Task<OutputDetailByEquipmentReportDto> Handle(GetOutputsDetailByEquipmentReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var validationError = ValidateAndResolveBranch(request, access, out var branchId);
        if (validationError != null)
            return validationError;

        var outputs = await LoadOutputsAsync(request, branchId, ct);
        var ordered = OrderSources(BuildSources(outputs, request));
        var items = mapper.Map<List<OutputDetailByEquipmentItemDto>>(ordered);

        var generatedBy = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => u.Fullname)
            .FirstOrDefaultAsync(ct);

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

    private OutputDetailByEquipmentReportDto? ValidateAndResolveBranch(
        GetOutputsDetailByEquipmentReportQuery request,
        AccessValidationResult<OutputDetailByEquipmentReportDto> access,
        out Guid branchId)
    {
        branchId = Guid.Empty;

        if (!request.From.HasValue || !request.To.HasValue)
            return _errorManager.ThrowBadRequest<OutputDetailByEquipmentReportDto>("Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");

        if (request.From.Value > request.To.Value)
            return _errorManager.ThrowBadRequest<OutputDetailByEquipmentReportDto>("El periodo no es valido.", "ERP:010");

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

        if (!isAdministrator && access.Profile == null)
            return _errorManager.ThrowBadRequest<OutputDetailByEquipmentReportDto>("Perfil no encontrado.", "ERP:011");

        if (isAdministrator && !request.BranchId.HasValue)
            return _errorManager.ThrowBadRequest<OutputDetailByEquipmentReportDto>("Debe especificar la sucursal.", "ERP:008");

        branchId = isAdministrator ? request.BranchId!.Value : access.Profile.BranchId;
        return null;
    }

    private async Task<List<ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs>> LoadOutputsAsync(GetOutputsDetailByEquipmentReportQuery request, Guid branchId, CancellationToken ct)
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

    private static List<OutputDetailByEquipmentSource> BuildSources(List<ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs> outputs, GetOutputsDetailByEquipmentReportQuery request)
    {
        var sources = new List<OutputDetailByEquipmentSource>();

        foreach (var output in outputs)
        {
            if (output.SupplyRequest?.Equipment != null)
            {
                foreach (var item in output.SupplyRequest.SupplyRequestItems)
                {
                    var product = item.ProductStock?.Product;
                    if (!MatchesFilters(product?.CategoryId, item.ProductStock?.ProductId, request))
                        continue;

                    sources.Add(new OutputDetailByEquipmentSource
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

                sources.Add(new OutputDetailByEquipmentSource
                {
                    Output = output,
                    Item = null,
                    IsFuel = true
                });
            }
        }

        return sources;
    }

    private static bool MatchesFilters(Guid? categoryId, Guid? productId, GetOutputsDetailByEquipmentReportQuery request)
    {
        if (request.CategoryId is { Count: > 0 } && !request.CategoryId.Contains(categoryId ?? Guid.Empty))
            return false;

        if (request.ProductIds is { Count: > 0 } && !request.ProductIds.Contains(productId ?? Guid.Empty))
            return false;

        return true;
    }

    private static List<OutputDetailByEquipmentSource> OrderSources(List<OutputDetailByEquipmentSource> sources)
    {
        return [.. sources
            .OrderBy(GetEquipmentCode)
            .ThenBy(GetCategoryName)
            .ThenBy(GetProductName)
            .ThenBy(s => s.Output.DispatchedAt ?? s.Output.CreatedAt)
            .ThenBy(s => s.Output.Code)];
    }

    private static string GetEquipmentCode(OutputDetailByEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.Equipment?.Code ?? ""
        : source.Output.SupplyRequest?.Equipment?.Code ?? "";

    private static string GetCategoryName(OutputDetailByEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.ProductStock?.Product?.Category?.Name ?? "Sin categoria"
        : source.Item?.ProductStock?.Product?.Category?.Name ?? "Sin categoria";

    private static string GetProductName(OutputDetailByEquipmentSource source) => source.IsFuel
        ? source.Output.FuelRequest?.ProductStock?.Product?.ProductName ?? ""
        : source.Item?.ProductStock?.Product?.ProductName ?? "";
}
