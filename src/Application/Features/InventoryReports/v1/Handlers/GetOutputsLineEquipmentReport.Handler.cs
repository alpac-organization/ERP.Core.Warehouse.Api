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

        if (!request.From.HasValue || !request.To.HasValue)
        {
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("Debe especificar el periodo (\"de\" y \"hasta\").", "ERP:007");
        }

        if (request.CategoryId is not { Count: > 0 })
        {
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("Debe enviar al menos una categoria.", "ERP:009");
        }

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;

        if (isAdministrator && !request.BranchId.HasValue)
        {
            return _errorManager.ThrowBadRequest<OutputLineEquipmentReportDto>("Debe especificar la sucursal.", "ERP:008");
        }

        var branchId = isAdministrator ? request.BranchId!.Value : access.Profile.BranchId;

        var outputs = await LoadOutputsAsync(request, branchId, ct);
        var sources = BuildSources(outputs, request);
        var ordered = OrderSources(sources);

        var report = new OutputLineEquipmentReportDto
        {
            From = request.From.Value,
            To = request.To.Value,
            Categories = [.. ordered
                .GroupBy(s => s.IsFuel
                    ? (s.Output.FuelRequest != null && s.Output.FuelRequest.ProductStock != null && s.Output.FuelRequest.ProductStock.Product?.Category != null ? s.Output.FuelRequest.ProductStock.Product.Category.Name : "Sin categoria")
                    : (s.Item != null && s.Item.ProductStock != null && s.Item.ProductStock.Product?.Category != null ? s.Item.ProductStock.Product.Category.Name : "Sin categoria"))
                .Select(categoryGroup => new OutputLineEquipmentCategoryDto
                {
                    CategoryName = categoryGroup.Key,
                    Equipments = [.. categoryGroup
                        .GroupBy(s => s.IsFuel
                            ? (s.Output.FuelRequest != null ? s.Output.FuelRequest.Equipment.Code : "Sin equipo")
                            : (s.Output.SupplyRequest != null && s.Output.SupplyRequest.Equipment != null ? s.Output.SupplyRequest.Equipment.Code : "Sin equipo"))
                        .Select(equipmentGroup =>
                        {
                            var items = equipmentGroup.Select(s => mapper.Map<OutputLineEquipmentItemDto>(s)).ToList();
                            var equipment = equipmentGroup.First().IsFuel
                                ? equipmentGroup.First().Output.FuelRequest?.Equipment
                                : equipmentGroup.First().Output.SupplyRequest?.Equipment;
                            return new OutputLineEquipmentEquipmentDto
                            {
                                EquipmentInformation = equipment != null ? mapper.Map<EquipmentInformation>(equipment) : new EquipmentInformation(),
                                Items = items,
                                TotalQuantity = items.Sum(i => i.Quantity),
                                TotalCost = items.Sum(i => i.Cost),
                                TotalTotalCost = items.Sum(i => i.TotalCost)
                            };
                        })]
                })]
        };

        return report;
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
            .Where(o => (o.DispatchedAt ?? o.CreatedAt) <= request.To!.Value);

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
            if (output.SupplyRequest != null)
            {
                foreach (var item in output.SupplyRequest.SupplyRequestItems)
                {
                    var product = item.ProductStock?.Product;
                    if (request.CategoryId is { Count: > 0 } && !request.CategoryId.Contains(product?.CategoryId ?? Guid.Empty))
                        continue;

                    sources.Add(new OutputLineEquipmentSource
                    {
                        Output = output,
                        Item = item,
                        IsFuel = false
                    });
                }
            }

            if (output.FuelRequest != null)
            {
                var product = output.FuelRequest.ProductStock?.Product;
                if (request.CategoryId is { Count: > 0 } && !request.CategoryId.Contains(product?.CategoryId ?? Guid.Empty))
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

    private static List<OutputLineEquipmentSource> OrderSources(List<OutputLineEquipmentSource> sources)
    {
        return [.. sources
            .OrderBy(s => (s.IsFuel
                ? (s.Output.FuelRequest != null && s.Output.FuelRequest.ProductStock != null && s.Output.FuelRequest.ProductStock.Product?.Category != null ? s.Output.FuelRequest.ProductStock.Product.Category.Name : "")
                : (s.Item != null && s.Item.ProductStock != null && s.Item.ProductStock.Product?.Category != null ? s.Item.ProductStock.Product.Category.Name : "")))
            .ThenBy(s => (s.IsFuel
                ? (s.Output.FuelRequest != null ? s.Output.FuelRequest.Equipment.Code : "")
                : (s.Output.SupplyRequest != null && s.Output.SupplyRequest.Equipment != null ? s.Output.SupplyRequest.Equipment.Code : "")))
            .ThenBy(s => s.Output.Branch != null ? s.Output.Branch.BranchCode : "")];
    }
}