using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public class GetAvailableWarehousesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetAvailableWarehousesQuery, List<AvailableWarehouseDto>>(unitOfWork, errorManager)
{
    public override async Task<List<AvailableWarehouseDto>> Handle(
        GetAvailableWarehousesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var warehousesQuery = _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Include(w => w.Sections)
                .ThenInclude(s => s.Racks)
                    .ThenInclude(r => r.Positions)
            .Include(w => w.Sections)
                .ThenInclude(s => s.Lots)
                    .ThenInclude(l => l.Positions)
            .Where(w => w.DeletedAt == null && w.IsActive);

        if (request.DocumentType.HasValue)
        {
            var allowedType = WarehouseAssignmentRules.AllowedWarehouseType(request.DocumentType.Value);
            warehousesQuery = warehousesQuery.Where(w => w.WarehouseType == allowedType);
        }

        var warehouses = await warehousesQuery.ToListAsync(cancellationToken);

        var occupiedRackPositions = (await _unitOfWork.WarehouseAssignments.Entities
                .AsNoTracking()
                .Where(a => a.DeletedAt == null && a.RackPositionsId != null)
                .Select(a => a.RackPositionsId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var occupiedLotsPositions = (await _unitOfWork.WarehouseAssignments.Entities
                .AsNoTracking()
                .Where(a => a.DeletedAt == null && a.LotsPositionsId != null)
                .Select(a => a.LotsPositionsId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        return warehouses.Select(w => new AvailableWarehouseDto
        {
            Id = w.Id,
            Code = w.Code,
            Name = w.WarehouseName,
            WarehouseType = w.WarehouseType,
            Sections = w.Sections
                .Where(s => s.DeletedAt == null && s.IsActive && s.StorageType != SectionStorageType.Empty)
                .Select(s => new AvailableSectionDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    StorageType = s.StorageType,
                    Racks = s.Racks
                        .Where(r => r.DeletedAt == null && r.Status == RackStatus.Available)
                        .Select(r => new AvailableRackDto
                        {
                            Id = r.Id,
                            Code = r.Code,
                            Status = r.Status,
                            Positions = (request.RackId.HasValue && request.RackId.Value == r.Id)
                                ? r.Positions
                                    .Where(p => p.DeletedAt == null && !p.IsBlocked && !occupiedRackPositions.Contains(p.Id))
                                    .Select(p => new AvailablePositionDto
                                    {
                                        Id = p.Id,
                                        PositionCode = p.PositionCode
                                    })
                                    .ToList()
                                : null
                        })
                        .ToList(),
                    Lots = s.Lots
                        .Where(l => l.DeletedAt == null && l.Status == RackStatus.Available)
                        .Select(l => new AvailableLotDto
                        {
                            Id = l.Id,
                            Code = l.Code,
                            Positions = (request.LotId.HasValue && request.LotId.Value == l.Id)
                                ? l.Positions
                                    .Where(p => p.DeletedAt == null && !p.IsBlocked && !occupiedLotsPositions.Contains(p.Id))
                                    .Select(p => new AvailablePositionDto
                                    {
                                        Id = p.Id,
                                        PositionCode = p.PositionCode
                                    })
                                    .ToList()
                                : null
                        })
                        .ToList()
                })
                .ToList()
        }).ToList();
    }
}

public class GetWarehouseMachineriesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetWarehouseMachineriesQuery, List<WarehouseMachineryDto>>(unitOfWork, errorManager)
{
    public override async Task<List<WarehouseMachineryDto>> Handle(
        GetWarehouseMachineriesQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        return await _unitOfWork.WarehouseMachineries.Entities
            .AsNoTracking()
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.Code)
            .Select(m => new WarehouseMachineryDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                MachineryType = m.MachineryType,
                IsActive = m.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}

public class GetWarehouseStaffsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetWarehouseStaffsQuery, List<WarehouseStaffDto>>(unitOfWork, errorManager)
{
    public override async Task<List<WarehouseStaffDto>> Handle(
        GetWarehouseStaffsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        return await _unitOfWork.WarehouseStaffs.Entities
            .AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.FullName)
            .Select(s => new WarehouseStaffDto
            {
                Id = s.Id,
                FullName = s.FullName,
                Role = s.Role,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}