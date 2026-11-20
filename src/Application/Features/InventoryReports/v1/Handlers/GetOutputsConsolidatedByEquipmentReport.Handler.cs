using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers;

/// <summary>
/// Genera el reporte de Salida Consolidado por Equipo: agrupacion por equipo/producto de las salidas.
/// </summary>
public class GetOutputsConsolidatedByEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetOutputsConsolidatedByEquipmentReportQuery, List<OutputConsolidatedByEquipmentReportDto>>(unitOfWork, errorManager)
{
    public override async Task<List<OutputConsolidatedByEquipmentReportDto>> Handle(GetOutputsConsolidatedByEquipmentReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var query = _unitOfWork.InventoryOutputs.Entities
            .AsNoTracking()
            .Include(o => o.Branch)
            .Include(o => o.CostCenter)
            .Include(o => o.SupplyRequest)
                .ThenInclude(s => s!.SupplyRequestItems)
                    .ThenInclude(i => i.ProductStock)
                        .ThenInclude(ps => ps!.Product)
                            .ThenInclude(p => p!.Category)
            .Include(o => o.SupplyRequest).ThenInclude(s => s!.Equipment)
            .Include(o => o.SupplyRequest).ThenInclude(s => s!.Customer)
            .Include(o => o.FuelRequest).ThenInclude(f => f!.ProductStock)
                .ThenInclude(ps => ps!.Product).ThenInclude(p => p!.Category)
            .Include(o => o.FuelRequest).ThenInclude(f => f!.Equipment)
            .Include(o => o.FuelRequest).ThenInclude(f => f!.Customer)
            .Where(o => o.DeletedAt == null);

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);
        if (request.From.HasValue)
            query = query.Where(o => (o.DispatchedAt ?? o.CreatedAt) >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(o => (o.DispatchedAt ?? o.CreatedAt) <= request.To.Value);
        if (request.EquipmentIds != null && request.EquipmentIds.Count > 0)
            query = query.Where(o => (o.SupplyRequest != null && o.SupplyRequest.EquipmentId.HasValue && request.EquipmentIds.Contains(o.SupplyRequest.EquipmentId.Value)) ||
                                    (o.FuelRequest != null && request.EquipmentIds.Contains(o.FuelRequest.EquipmentId)));

        var outputs = await query.ToListAsync(ct);

        var sources = new List<OutputDetailByEquipmentSource>();

        foreach (var o in outputs)
        {
            if (o.SupplyRequest != null)
            {
                foreach (var item in o.SupplyRequest.SupplyRequestItems)
                {
                    sources.Add(new OutputDetailByEquipmentSource
                    {
                        Output = o,
                        Item = item,
                        IsFuel = false
                    });
                }
            }

            if (o.FuelRequest != null)
            {
                sources.Add(new OutputDetailByEquipmentSource
                {
                    Output = o,
                    Item = null,
                    IsFuel = true
                });
            }
        }

        var rows = mapper.Map<List<OutputDetailByEquipmentReportDto>>(sources);

        var consolidated = rows
            .GroupBy(r => new { r.Equipment, r.Category, r.StockCode, r.ProductCode, r.ProductName, r.CostCenter, r.Customer })
            .Select(g => new OutputConsolidatedByEquipmentReportDto
            {
                Equipment = g.Key.Equipment,
                Category = g.Key.Category,
                StockCode = g.Key.StockCode,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                Quantity = g.Sum(x => x.Quantity),
                TotalCost = g.Sum(x => x.TotalCost),
                CostCenter = g.Key.CostCenter,
                Customer = g.Key.Customer
            })
            .OrderBy(r => r.Equipment).ThenBy(r => r.Category)
            .ToList();

        return consolidated;
    }
}