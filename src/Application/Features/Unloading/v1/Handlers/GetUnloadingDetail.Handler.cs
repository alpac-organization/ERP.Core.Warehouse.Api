using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Handlers;

public class GetUnloadingDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetUnloadingDetailQuery, UnloadingDetailDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<UnloadingDetailDto> Handle(
        GetUnloadingDetailQuery request,
        CancellationToken cancellationToken)
    {
        #region Validar acceso
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;
        #endregion

        #region Obtener descarga y asignación relacionada
        var detail = await _unitOfWork.UnloadingDetails.Entities
            .AsNoTracking()
            .AsSplitQuery()
            .Where(d => d.Id == request.UnloadingId && d.DeletedAt == null)
            .Include(d => d.WarehouseAssignment)
            .Include(d => d.UnloadingPallets.Where(p => p.DeletedAt == null))
            .Include(d => d.UnloadingSupplies.Where(s => s.DeletedAt == null))
                .ThenInclude(s => s.Supplies)
            .FirstOrDefaultAsync(cancellationToken);

        if (detail is null)
        {
            return _errorManager.ThrowBadRequest<UnloadingDetailDto>(
                "La descarga aún no ha sido iniciada para la asignación indicada.",
                "ERP:UNLOADING_NOT_STARTED");
        }
        #endregion

        #region Obtener inicio y reservas
        var startLog = await _unitOfWork.StepExecutionLogs.Entities
            .AsNoTracking()
            .Where(l => l.RecordEntranceId == detail.WarehouseAssignment.RecordEntranceId &&
                        l.WorkflowStepDefinitionCode == WorkflowStepCodes.Unloading &&
                        l.DeletedAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var reservations = await _unitOfWork.UnloadingPositionsReservations.Entities
                .AsNoTracking()
                .Where(r => r.WarehouseAssignmentId == detail.WarehouseAssignmentId && r.DeletedAt == null)
                .ToListAsync(cancellationToken);
        #endregion

        #region Obtener códigos de posición
        var rackIds = reservations.Where(r => r.RackPositionId.HasValue)
            .Select(r => r.RackPositionId!.Value).ToList();
        var lotIds = reservations.Where(r => r.LotPositionId.HasValue)
            .Select(r => r.LotPositionId!.Value).ToList();
        var positionCodes = await _unitOfWork.RackPositions.Entities
            .AsNoTracking()
            .Where(p => rackIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PositionCode, cancellationToken);
        var lotCodes = await _unitOfWork.LotsPositions.Entities
            .AsNoTracking()
            .Where(p => lotIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.PositionCode, cancellationToken);
        foreach (var code in lotCodes)
            positionCodes[code.Key] = code.Value;
        #endregion

        #region Mapear respuesta
        return _mapper.Map<UnloadingDetailDto>(detail, opts =>
        {
            opts.Items["StartLog"] = startLog;
            opts.Items["Reservations"] = reservations;
            opts.Items["PositionCodes"] = positionCodes;
        });
        #endregion
    }
}
