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
/// Genera el reporte de Salida a Detalle por Equipo: desglose de salidas por producto y equipo.
/// </summary>
public class GetOutputsDetailByEquipmentReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetOutputsDetailByEquipmentReportQuery, List<OutputDetailByEquipmentReportDto>>(unitOfWork, errorManager)
{
    public override async Task<List<OutputDetailByEquipmentReportDto>> Handle(GetOutputsDetailByEquipmentReportQuery request, CancellationToken ct)
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
            .Include(o => o.SupplyRequest).ThenInclude(s => s!.OperationalOrder)
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

        return mapper.Map<List<OutputDetailByEquipmentReportDto>>(sources)
            .OrderBy(r => r.Date).ThenBy(r => r.OutputCode).ToList();
    }
}