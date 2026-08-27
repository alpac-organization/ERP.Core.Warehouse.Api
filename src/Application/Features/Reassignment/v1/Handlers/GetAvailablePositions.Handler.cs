using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class GetAvailablePositionsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetAvailablePositionsQuery, List<AvailablePositionDto>>(unitOfWork, errorManager)
{
    public override async Task<List<AvailablePositionDto>> Handle(
        GetAvailablePositionsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var rackPositions = await GetPositionRowsAsync(request, cancellationToken);
        var lotPositions = await GetLotPositionRowsAsync(request, cancellationToken);

        return [.. rackPositions, .. lotPositions];
    }

    private async Task<List<AvailablePositionDto>> GetPositionRowsAsync(
        GetAvailablePositionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.RackPositions.Entities
            .AsNoTracking()
            .Where(r => r.DeletedAt == null
                && r.Rack.Section.WarehouseId == request.WarehouseId);

        if (request.SectionId.HasValue)
            query = query.Where(r => r.Rack.SectionId == request.SectionId.Value);

        var rows = await query
            .OrderBy(r => r.Rack.Section.Code)
            .ThenBy(r => r.Rack.Code)
            .ThenBy(r => r.PositionNumber)
            .ToListAsync(cancellationToken);

        return ApplyStatusFilter(request.Status, rows
            .Select(r => ReassignmentMapper.ToAvailablePositionDto
                (r, ResolveStockId(r.Id), ResolveReservedBySessionId(r.Id))));
    }

    private async Task<List<AvailablePositionDto>> GetLotPositionRowsAsync(
        GetAvailablePositionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.LotsPositions.Entities
            .AsNoTracking()
            .Where(l => l.DeletedAt == null
                && l.Lot.Section.WarehouseId == request.WarehouseId);

        if (request.SectionId.HasValue)
            query = query.Where(l => l.Lot.SectionId == request.SectionId.Value);

        var rows = await query
            .OrderBy(l => l.Lot.Section.Code)
            .ThenBy(l => l.Lot.Code)
            .ThenBy(l => l.RowNumber)
            .ThenBy(l => l.ColumnNumber)
            .ToListAsync(cancellationToken);

        return ApplyStatusFilter(request.Status, rows
            .Select(l => ReassignmentMapper.ToAvailablePositionDto
                (l, ResolveStockId(l.Id), ResolveReservedBySessionId(l.Id))));
    }

    private static List<AvailablePositionDto> ApplyStatusFilter(
        string? status,
        IEnumerable<AvailablePositionDto> positions)
    {
        var result = positions.ToList();
        if (status is not null)
            result = result.Where(p => p.Status == status).ToList();
        return result;
    }

    private Guid? ResolveStockId(Guid positionId)
        => _unitOfWork.StockPlacements.Entities
            .Where(s => (s.RackPositionId == positionId || s.LotPositionId == positionId)
                && s.VacatedAtDate == null && s.DeletedAt == null)
            .Select(s => (Guid?)s.StockId)
            .FirstOrDefault();

    private Guid? ResolveReservedBySessionId(Guid positionId)
        => _unitOfWork.ReassignmentMemoryItems.Entities
            .Where(m => (m.TargetRackPositionId == positionId || m.TargetLotPositionId == positionId)
                && m.ResolvedAtDate == null && m.DeletedAt == null)
            .Select(m => (Guid?)m.ReassignmentSessionId)
            .FirstOrDefault();
}
