using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public class CreateUnloadingDetailsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CreateUnloadingDetailsCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateUnloadingDetailsCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.Assignment)
            .Include(r => r.UnloadingDetails)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        if (recordEntrance.Assignment == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Debe asignar una bodega antes de registrar los detalles de descarga.",
                "ERP:WAREHOUSE_ASSIGNMENT_REQUIRED");
        }

        if (recordEntrance.UnloadingDetails != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Los detalles de descarga de esta recepción ya fueron registrados.",
                "ERP:UNLOADING_DETAILS_ALREADY_EXISTS");
        }

        var unloadingDetails = new UnloadingDetails
        {
            Id = Guid.NewGuid(),
            RecordEntranceId = recordEntrance.Id,
            WarehouseAssignmentsId = recordEntrance.Assignment.Id,
            UnloadingStartTime = request.UnloadingStartTime,
            UnloadingEndTime = null,
            WarehouseChiefUserId = request.WarehouseChiefUserId,
            PreparedPallets = request.PreparedPallets
        };

        await _unitOfWork.UnloadingDetails.InsertUnloadingDetails(unloadingDetails);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public class CreateUnloadingCrewHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CreateUnloadingCrewCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateUnloadingCrewCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.UnloadingDetails)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        if (recordEntrance.UnloadingDetails == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Debe registrar primero los detalles de descarga para asignar la cuadrilla.",
                "ERP:UNLOADING_DETAILS_REQUIRED");
        }

        if (request.PersonaCount <= 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La cantidad de personas de la cuadrilla debe ser mayor a cero.",
                "ERP:INVALID_PERSONA_COUNT");
        }

        var crewAssignment = new UnloadingCrewAssignments
        {
            Id = Guid.NewGuid(),
            UnloadingDetailsId = recordEntrance.UnloadingDetails.Id,
            AssignedAt = NicaraguaClock.Now,
            PersonaCount = request.PersonaCount,
            Tecerizada = request.Tecerizada
        };

        await _unitOfWork.UnloadingCrewAssignments.InsertUnloadingCrewAssignments(crewAssignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public class CreateUnloadingMachineryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CreateUnloadingMachineryCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateUnloadingMachineryCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.UnloadingDetails)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        if (recordEntrance.UnloadingDetails == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Debe registrar primero los detalles de descarga para asignar maquinaria.",
                "ERP:UNLOADING_DETAILS_REQUIRED");
        }

        var machinery = await _unitOfWork.WarehouseMachineries.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MachineryCode && m.DeletedAt == null, cancellationToken);

        if (machinery == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La maquinaria seleccionada no existe.",
                "ERP:MACHINERY_NOT_FOUND");
        }

        if (!machinery.IsActive)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "La maquinaria seleccionada se encuentra inactiva.",
                "ERP:MACHINERY_INACTIVE");
        }

        var machineryAssignment = new UnloadingMachineryAssignments
        {
            Id = Guid.NewGuid(),
            UnloadingDetailsId = recordEntrance.UnloadingDetails.Id,
            MachineryCode = machinery.Id,
            StartTime = request.StartTime,
            EndTime = null,
            AssignedByUserId = request.UserId.ToString()
        };

        await _unitOfWork.UnloadingMachineryAssignments.InsertUnloadingMachineryAssignments(machineryAssignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}