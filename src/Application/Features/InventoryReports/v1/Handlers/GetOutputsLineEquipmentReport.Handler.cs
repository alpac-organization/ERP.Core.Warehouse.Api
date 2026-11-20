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
/// Genera el reporte de Salidas Linea-Equipo: detalle de salidas por producto, ordenado por categoria, equipo y sucursal.
/// </summary>
public class GetOutputsLineEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetOutputsLineEquipmentReportQuery, List<OutputLineEquipmentReportDto>>(unitOfWork, errorManager)
{
    public override async Task<List<OutputLineEquipmentReportDto>> Handle(GetOutputsLineEquipmentReportQuery request, CancellationToken ct)
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
            .Where(o => o.DeletedAt == null);

        if (request.BranchId.HasValue)
            query = query.Where(o => o.BranchId == request.BranchId.Value);

        if (request.From.HasValue)
            query = query.Where(o => (o.DispatchedAt ?? o.CreatedAt) >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(o => (o.DispatchedAt ?? o.CreatedAt) <= request.To.Value);

        if (request.EquipmentId.HasValue)
            query = query.Where(o => (o.SupplyRequest != null && o.SupplyRequest.EquipmentId == request.EquipmentId.Value) ||
                                    (o.FuelRequest != null && o.FuelRequest.EquipmentId == request.EquipmentId.Value));

        var outputs = await query.ToListAsync(ct);

        var sources = new List<OutputLineEquipmentSource>();

        foreach (var o in outputs)
        {
            if (o.SupplyRequest != null)
            {
                foreach (var item in o.SupplyRequest.SupplyRequestItems)
                {
                    var product = item.ProductStock?.Product;
                    if (request.CategoryId.HasValue && product?.CategoryId != request.CategoryId.Value) continue;

                    sources.Add(new OutputLineEquipmentSource
                    {
                        Output = o,
                        Item = item,
                        IsFuel = false
                    });
                }
            }

            if (o.FuelRequest != null)
            {
                var product = o.FuelRequest.ProductStock?.Product;
                if (request.CategoryId.HasValue && product?.CategoryId != request.CategoryId.Value) continue;

                sources.Add(new OutputLineEquipmentSource
                {
                    Output = o,
                    Item = null,
                    IsFuel = true
                });
            }
        }

        var ordered = sources
            .OrderBy(s => (s.IsFuel
                ? (s.Output.FuelRequest != null && s.Output.FuelRequest.ProductStock != null && s.Output.FuelRequest.ProductStock.Product.Category != null ? s.Output.FuelRequest.ProductStock.Product.Category.Name : "")
                : (s.Item != null && s.Item.ProductStock != null && s.Item.ProductStock.Product.Category != null ? s.Item.ProductStock.Product.Category.Name : "")))
            .ThenBy(s => (s.IsFuel
                ? (s.Output.FuelRequest != null ? s.Output.FuelRequest.Equipment.Code : "")
                : (s.Output.SupplyRequest != null && s.Output.SupplyRequest.Equipment != null ? s.Output.SupplyRequest.Equipment.Code : "")))
            .ThenBy(s => s.Output.Branch != null ? s.Output.Branch.BranchCode : "")
            .ToList();

        return mapper.Map<List<OutputLineEquipmentReportDto>>(ordered);
    }
}