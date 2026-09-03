using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Handlers;

public class ReserveUnloadingPositionsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<ReserveUnloadingPositionsCommand, bool>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<bool> Handle(
        ReserveUnloadingPositionsCommand request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 1. Obtener y validar la asignación
        var assignment = await _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .Where(a => a.Id == request.AssignmentId && a.DeletedAt == null)
            .Select(a => new { a.Id, a.EntranceDucatId, a.WarehouseId, a.UnloadingStatus })
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La asignación no fue encontrada o ya ha sido eliminada.",
                "ERP:ASSIGNMENT_NOT_FOUND");
        }

        if (assignment.UnloadingStatus != UnloadingStatus.InProgress || assignment.EntranceDucatId is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La asignación no está en descarga; no se pueden reservar posiciones.",
                "ERP:ASSIGNMENT_NOT_ACTIVE");
        }
        #endregion

        #region 2. Obtener la descarga y el total de polines declarados
        var unloading = await _unitOfWork.UnloadingDetails.Entities
            .AsNoTracking()
            .Where(d => d.WarehouseAssignmentId == request.AssignmentId && d.DeletedAt == null)
            .Select(d => new { d.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (unloading is null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se encontró una descarga en curso para la asignación.",
                "ERP:UNLOADING_NOT_STARTED");
        }

        var declaredTotal = await _unitOfWork.UnloadingPallets.Entities
            .AsNoTracking()
            .Where(p => p.UnloadingDetailsId == unloading.Id && p.DeletedAt == null)
            .Select(p => p.Quantity)
            .SumAsync(cancellationToken);
        #endregion

        #region 2.5. Validar que la descarga no tenga ya posiciones reservadas
        var alreadyReserved = await _unitOfWork.UnloadingPositionsReservations.Entities
            .AsNoTracking()
            .AnyAsync(r => r.UnloadingDetailsId == unloading.Id && r.DeletedAt == null, cancellationToken);

        if (alreadyReserved)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La descarga ya tiene posiciones reservadas; no se pueden volver a reservar.",
                "ERP:POSITIONS_ALREADY_RESERVED");
        }
        #endregion

        #region 3. Validar que el número de posiciones iguale al de polines declarados (un polín por posición)
        if (request.Positions.Count != declaredTotal)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"El número de posiciones ({request.Positions.Count}) debe ser igual al total de polines declarados ({declaredTotal}).",
                "ERP:RESERVATION_COUNT_MISMATCH");
        }
        #endregion

        #region 4. Primera pasada: validar unicidad, existencia y disponibilidad de todas las posiciones (sin mutar ni insertar)
        var seen = new HashSet<Guid>();
        var validated = new List<ValidatedPosition>();

        foreach (var item in request.Positions)
        {
            var positionId = item.RackPositionId ?? item.LotPositionId;

            if (positionId is not null && !seen.Add(positionId.Value))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Una posición no puede reservarse más de una vez por descarga.",
                    "ERP:DUPLICATE_POSITION");
            }

            validated.Add(await ValidateFreePositionAsync(item, unloading.Id, cancellationToken));
        }
        #endregion

        var now = NicaraguaClock.Now;
        var nowDate = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);

        #region 5. Segunda pasada: marcar reservada e insertar reservas (sin más consultas)
        for (var i = 0; i < request.Positions.Count; i++)
        {
            var item = request.Positions[i];
            var position = validated[i];

            if (position.rack is RackPositions rackTarget)
            {
                rackTarget.IsReserved = true;
            }
            else if (position.lot is LotsPositions lotTarget)
            {
                lotTarget.IsReserved = true;
            }

            var entity = _mapper.Map<UnloadingPositionReservations>(item, opts =>
            {
                opts.Items["EntranceDucatId"] = assignment.EntranceDucatId;
                opts.Items["WarehouseAssignmentId"] = assignment.Id;
                opts.Items["WarehouseId"] = assignment.WarehouseId;
                opts.Items["UnloadingDetailsId"] = unloading.Id;
                opts.Items["ReservedByUserId"] = request.UserId.ToString();
                opts.Items["ReservedAtDate"] = nowDate;
                opts.Items["ReservedAtTime"] = nowTime;
                opts.Items["Quantity"] = 1;
            });

            await _unitOfWork.UnloadingPositionsReservations.InsertPositionReservation(entity);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return true;
    }

    private async Task<ValidatedPosition> ValidateFreePositionAsync(PositionReservationItemDto item, Guid unloadingDetailsId, CancellationToken ct)
    {
        if (item.RackPositionId.HasValue)
        {
            var target = await _unitOfWork.RackPositions.Entities
                .FirstOrDefaultAsync(p => p.Id == item.RackPositionId.Value, ct);

            if (target is null)
            {
                _errorManager.ThrowNotFound<object>(
                    $"La posición destino rack {item.RackPositionId} no existe.",
                    "ERP:TARGET_POSITION_NOT_FOUND");
                return ValidatedPosition.Empty();
            }

            await ValidateAvailabilityAsync(
                target.IsOccupied, target.IsReserved, target.IsBlocked,
                "rack", target.PositionCode, target.Id, unloadingDetailsId, ct);

            return new ValidatedPosition(target, null);
        }

        if (item.LotPositionId.HasValue)
        {
            var target = await _unitOfWork.LotsPositions.Entities
                .FirstOrDefaultAsync(p => p.Id == item.LotPositionId.Value, ct);

            if (target is null)
            {
                _errorManager.ThrowNotFound<object>(
                    $"La posición destino tramo {item.LotPositionId} no existe.",
                    "ERP:TARGET_POSITION_NOT_FOUND");
                return ValidatedPosition.Empty();
            }

            await ValidateAvailabilityAsync(
                target.IsOccupied, target.IsReserved, target.IsBlocked,
                "tramo", target.PositionCode, target.Id, unloadingDetailsId, ct);

            return new ValidatedPosition(null, target);
        }

        _errorManager.ThrowBadRequest<object>(
            "Cada posición debe indicar una posición de rack o de lot.",
            "ERP:TARGET_POSITION_REQUIRED");
        return ValidatedPosition.Empty();
    }

    private async Task ValidateAvailabilityAsync(
        bool isOccupied, bool isReserved, bool isBlocked,
        string kind, string code, Guid positionId, Guid unloadingDetailsId, CancellationToken ct)
    {
        if (isBlocked)
        {
            _errorManager.ThrowBadRequest<object>(
                $"La posición destino {kind} {code} ({positionId}) está bloqueada.",
                "ERP:POSITION_BLOCKED");
            return;
        }

        if (isOccupied)
        {
            _errorManager.ThrowBadRequest<object>(
                $"La posición destino {kind} {code} ({positionId}) está ocupada.",
                "ERP:POSITION_OCCUPIED");
            return;
        }

        if (!isReserved) return;

        var existing = await _unitOfWork.UnloadingPositionsReservations.Entities
            .AsNoTracking()
            .Where(r => r.DeletedAt == null && (r.RackPositionId == positionId || r.LotPositionId == positionId))
            .Select(r => new { r.UnloadingDetailsId })
            .FirstOrDefaultAsync(ct);

        if (existing is not null && existing.UnloadingDetailsId == unloadingDetailsId)
        {
            _errorManager.ThrowBadRequest<object>(
                $"La posición destino {kind} {code} ({positionId}) ya está reservada por esta misma descarga.",
                "ERP:POSITION_RESERVED_BY_SAME_UNLOADING");
            return;
        }

        if (existing is not null)
        {
            _errorManager.ThrowBadRequest<object>(
                $"La posición destino {kind} {code} ({positionId}) está reservada por otra descarga.",
                "ERP:POSITION_RESERVED_BY_ANOTHER_UNLOADING");
            return;
        }

        _errorManager.ThrowBadRequest<object>(
            $"La posición destino {kind} {code} ({positionId}) está reservada u ocupada por otro proceso.",
            "ERP:POSITION_RESERVED_BY_ANOTHER_PROCESS");
    }

    private sealed record ValidatedPosition(RackPositions? rack, LotsPositions? lot)
    {
        public static ValidatedPosition Empty() => new(null, null);
    }
}