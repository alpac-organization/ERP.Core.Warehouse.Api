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
/// Genera el reporte de Levantamiento Fisico: existencias del sistema con campos para captura fisica, diferencia y observaciones.
/// </summary>
public class GetPhysicalCountReportHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetPhysicalCountReportQuery, List<PhysicalCountReportDto>>(unitOfWork, errorManager)
{
    public override async Task<List<PhysicalCountReportDto>> Handle(GetPhysicalCountReportQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var isAdministrator = access.Role?.RoleType == RoleType.Administrator;
        var isManager = access.Role?.RoleType == RoleType.Manager;

        var query = _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => ps.Product.ProductUsageType == ProductUsageType.Operational)
            .Where(ps => ps.Product.ProductStatus == ProductStatus.Active ||
                         ps.Product.ProductStatus == ProductStatus.LowProcess);

        if (isAdministrator)
        {
            if (request.BranchId.HasValue)
                query = query.Where(ps => ps.Product.BranchId == request.BranchId.Value);
            if (request.CostCenterId.HasValue)
                query = query.Where(ps => ps.CostCenterId == request.CostCenterId.Value);
        }
        else if (isManager)
        {
            query = query.Where(ps => ps.Product.BranchId == access.Profile.BranchId);
            if (request.CostCenterId.HasValue)
                query = query.Where(ps => ps.CostCenterId == request.CostCenterId.Value);
        }
        else
        {
            query = query.Where(ps => ps.Product.BranchId == access.Profile.BranchId)
                         .Where(ps => ps.CostCenterId == access.Profile.CostCenterId)
                         .Where(ps => ps.Product.CostCenter.WorkAreaId == access.Profile.AreaId);
        }

        if (request.AreaId.HasValue)
            query = query.Where(ps => ps.Product.CostCenter.WorkAreaId == request.AreaId.Value);

        if (request.BranchId.HasValue && !isAdministrator && !isManager)
            query = query.Where(ps => ps.Product.BranchId == request.BranchId.Value);

        if (request.CostCenterId.HasValue)
            query = query.Where(ps => ps.CostCenterId == request.CostCenterId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(ps => ps.Product.CategoryId == request.CategoryId.Value);

        if (!string.IsNullOrEmpty(request.ProductCode))
            query = query.Where(ps => ps.Product.Code == request.ProductCode);

        if (!string.IsNullOrEmpty(request.ProductName))
            query = query.Where(ps => EF.Functions.Like(ps.Product.ProductName!, $"%{request.ProductName}%"));

        var stocks = await query
            .Include(ps => ps.Product)
            .Include(ps => ps.CostCenter)
                .ThenInclude(cc => cc.WorkArea)
            .OrderBy(ps => ps.Product.CostCenter.WorkArea.WorkAreaName)
            .ThenBy(ps => ps.CostCenter.CostCenterName)
            .ThenBy(ps => ps.Product.Code)
            .ToListAsync(ct);

        return mapper.Map<List<PhysicalCountReportDto>>(stocks);
    }
}