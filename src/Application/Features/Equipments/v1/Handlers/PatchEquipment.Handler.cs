using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Handlers;

public class PatchEquipmentHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<PatchEquipmentCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(PatchEquipmentCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

        if (!access.IsSuccess)
        {
            return access.ErrorResponse!;
        }

        if (access.Role?.RoleType is not RoleType.Administrator)
            return _errorManager.ThrowBadRequest<Unit>("No tienes acceso para realizar esta acción", "ERP:INVALID_ACCESS");

        var equipment = await _unitOfWork.Equipments.Entities
            .Where(e => e.Id == request.EquipmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (equipment is null)
        {
            return _errorManager.ThrowNotFound<Unit>("El equipo no existe.", "ERP:01");
        }

        // Validar que la sucursal actual del equipo pertenezca a la compañía
        var currentBranch = await _unitOfWork.Branches.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == equipment.BranchId, cancellationToken);

        if (currentBranch is null || currentBranch.CompanyId != request.CompanyId)
            return _errorManager.ThrowBadRequest<Unit>("La sucursal no pertenece a la compañía.", "ERP:BRANCH_COMPANY_MISMATCH");

        // Si se envía una nueva sucursal, validar existencia y compañía
        if (request.BranchId.HasValue)
        {
            var newBranch = await _unitOfWork.Branches.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == request.BranchId.Value, cancellationToken);

            if (newBranch is null)
                return _errorManager.ThrowBadRequest<Unit>("La sucursal no existe.", "ERP:BRANCH_NOT_FOUND");

            if (newBranch.CompanyId != request.CompanyId)
                return _errorManager.ThrowBadRequest<Unit>("La sucursal no pertenece a la compañía.", "ERP:BRANCH_COMPANY_MISMATCH");
        }

        // Unicidad de código
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeExists = await _unitOfWork.Equipments.Entities
                .AnyAsync(e => e.Code == request.Code && e.Id != request.EquipmentId, cancellationToken);
            if (codeExists)
                return _errorManager.ThrowBadRequest<Unit>("El código del equipo ya existe.", "ERP:EQUIPMENT_CODE_DUPLICATED");
        }

        // Unicidad de número de serie
        if (!string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            var serialExists = await _unitOfWork.Equipments.Entities
                .AnyAsync(e => e.SerialNumber == request.SerialNumber && e.Id != request.EquipmentId, cancellationToken);
            if (serialExists)
                return _errorManager.ThrowBadRequest<Unit>("El número de serie del equipo ya existe.", "ERP:EQUIPMENT_SERIAL_DUPLICATED");
        }

        // Banderas resultantes: al menos una debe quedar activa
        var finalGasoline = request.Gasoline ?? equipment.Gasoline;
        var finalDiesel = request.Diesel ?? equipment.Diesel;
        var finalElectric = request.Electric ?? equipment.Electric;
        var finalLpg = request.Lpg ?? equipment.Lpg;

        if (!(finalGasoline || finalDiesel || finalElectric || finalLpg))
            return _errorManager.ThrowBadRequest<Unit>("Debe quedar al menos una bandera de combustible activa.", "ERP:EQUIPMENT_FUEL_FLAG_REQUIRED");

        EquipmentProfile.ApplyPatch(equipment, request);

        // Auditoría
        equipment.UpdatedByUserId = request.UserId;
        equipment.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return _errorManager.ThrowBadRequest<Unit>("El código o número de serie del equipo ya existe.", "ERP:EQUIPMENT_DUPLICATED");
        }

        return Unit.Value;
    }
}