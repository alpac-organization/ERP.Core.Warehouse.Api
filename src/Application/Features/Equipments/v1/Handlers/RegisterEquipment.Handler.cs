using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Handlers;
public class RegisterEquipmentHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ILogger<RegisterEquipmentHandler> logger) : BaseValidatorHandler<EquipmentCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(EquipmentCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚀Iniciando registro de equipo.");

        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType is not RoleType.Administrator)
            return _errorManager.ThrowBadRequest<Unit>("No tienes acceso para realizar esta acción", "ERP:INVALID_ACCESS");

        var validationError = await ValidateBeforeCreateAsync(request, cancellationToken);
        if (validationError.HasValue)
            return validationError.Value;

        var equipment = EquipmentProfile.ToEquipmentEntity(request, request.BranchId);

        try
        {
            await _unitOfWork.Equipments.RegisterEquipment(equipment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return _errorManager.ThrowBadRequest<Unit>("El código o número de serie del equipo ya existe.", "ERP:EQUIPMENT_DUPLICATED");
        }

        logger.LogInformation("Equipo registrado con éxito✅");
        return Unit.Value;
    }

    private async Task<Unit?> ValidateBeforeCreateAsync(EquipmentCommand request, CancellationToken cancellationToken)
    {
        // Sucursal existe y pertenece a la misma compañía
        var branch = await _unitOfWork.Branches.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);

        if (branch is null)
            return _errorManager.ThrowBadRequest<Unit>("La sucursal no existe.", "ERP:BRANCH_NOT_FOUND");

        if (branch.CompanyId != request.CompanyId)
            return _errorManager.ThrowBadRequest<Unit>("La sucursal no pertenece a la compañía.", "ERP:BRANCH_COMPANY_MISMATCH");

        // Unicidad de código
        if (await _unitOfWork.Equipments.Entities.AnyAsync(e => e.Code == request.Code, cancellationToken))
            return _errorManager.ThrowBadRequest<Unit>("El código del equipo ya existe.", "ERP:EQUIPMENT_CODE_DUPLICATED");

        // Unicidad de número de serie
        if (await _unitOfWork.Equipments.Entities.AnyAsync(e => e.SerialNumber == request.SerialNumber, cancellationToken))
            return _errorManager.ThrowBadRequest<Unit>("El número de serie del equipo ya existe.", "ERP:EQUIPMENT_SERIAL_DUPLICATED");

        return null;
    }
}