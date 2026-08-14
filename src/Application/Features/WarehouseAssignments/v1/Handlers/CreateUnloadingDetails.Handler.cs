using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;
using WarehouseAssignmentEntity = ERP.Core.Database.Domain.Entities.Warehouse.WarehouseAssignments;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public class CreateUnloadingDetailsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CreateUnloadingDetailsCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateUnloadingDetailsCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var context = await WarehouseDocumentLookup.FindDocumentAsync(
            _unitOfWork, request.DocumentId, request.DocumentType, cancellationToken);

        if (context == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El documento no fue encontrado o ya ha sido eliminado.",
                "ERP:DOCUMENT_NOT_FOUND");
        }

        var assignment = await GetAssignmentForDocumentAsync(request, cancellationToken);

        if (assignment == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Debe asignar una bodega antes de registrar los detalles de descarga.",
                "ERP:WAREHOUSE_ASSIGNMENT_REQUIRED");
        }

        var existingDetails = await _unitOfWork.UnloadingDetails.Entities
            .AnyAsync(u => u.WarehouseAssignmentsId == assignment.Id && u.DeletedAt == null, cancellationToken);

        if (existingDetails)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Los detalles de descarga de este documento ya fueron registrados.",
                "ERP:UNLOADING_DETAILS_ALREADY_EXISTS");
        }

        var unloadingDetails = new UnloadingDetails
        {
            Id = Guid.NewGuid(),
            RecordEntranceId = context.RecordEntrance.Id,
            WarehouseAssignmentsId = assignment.Id,
            UnloadingStartTime = request.UnloadingStartTime,
            UnloadingEndTime = null,
            WarehouseChiefUserId = request.WarehouseChiefUserId,
            PreparedPallets = request.PreparedPallets
        };

        await _unitOfWork.UnloadingDetails.InsertUnloadingDetails(unloadingDetails);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    internal static async Task<WarehouseAssignmentEntity?> GetAssignmentForDocumentAsync(
        IUnitOfWork unitOfWork,
        Guid documentId,
        DocumentType documentType,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.WarehouseAssignments.Entities
            .FirstOrDefaultAsync(a => a.DeletedAt == null &&
                (documentType == DocumentType.DUCA
                    ? a.EntranceDucatId == documentId
                    : a.CustomsDeclarationId == documentId), cancellationToken);
    }

    private async Task<WarehouseAssignmentEntity?> GetAssignmentForDocumentAsync(
        CreateUnloadingDetailsCommand request,
        CancellationToken cancellationToken)
    {
        return await GetAssignmentForDocumentAsync(_unitOfWork, request.DocumentId, request.DocumentType, cancellationToken);
    }
}

public class CreateUnloadingCrewHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CreateUnloadingCrewCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateUnloadingCrewCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var unloadingDetails = await GetUnloadingDetailsAsync(request, cancellationToken);

        if (unloadingDetails == null)
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
            UnloadingDetailsId = unloadingDetails.Id,
            AssignedAt = NicaraguaClock.Now,
            PersonaCount = request.PersonaCount,
            Tecerizada = request.Tecerizada
        };

        await _unitOfWork.UnloadingCrewAssignments.InsertUnloadingCrewAssignments(crewAssignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    internal static async Task<UnloadingDetails?> GetUnloadingDetailsAsync(
        IUnitOfWork unitOfWork,
        Guid documentId,
        DocumentType documentType,
        CancellationToken cancellationToken)
    {
        var assignment = await CreateUnloadingDetailsHandler.GetAssignmentForDocumentAsync(
            unitOfWork, documentId, documentType, cancellationToken);

        if (assignment == null) return null;

        return await unitOfWork.UnloadingDetails.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.WarehouseAssignmentsId == assignment.Id && u.DeletedAt == null, cancellationToken);
    }

    private async Task<UnloadingDetails?> GetUnloadingDetailsAsync(
        CreateUnloadingCrewCommand request,
        CancellationToken cancellationToken)
    {
        return await GetUnloadingDetailsAsync(_unitOfWork, request.DocumentId, request.DocumentType, cancellationToken);
    }
}

public class CreateUnloadingMachineryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<CreateUnloadingMachineryCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateUnloadingMachineryCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var unloadingDetails = await GetUnloadingDetailsAsync(request, cancellationToken);

        if (unloadingDetails == null)
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
            UnloadingDetailsId = unloadingDetails.Id,
            MachineryCode = machinery.Id,
            StartTime = request.StartTime,
            EndTime = null,
            AssignedByUserId = request.UserId.ToString()
        };

        await _unitOfWork.UnloadingMachineryAssignments.InsertUnloadingMachineryAssignments(machineryAssignment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<UnloadingDetails?> GetUnloadingDetailsAsync(
        CreateUnloadingMachineryCommand request,
        CancellationToken cancellationToken)
    {
        return await CreateUnloadingCrewHandler.GetUnloadingDetailsAsync(
            _unitOfWork, request.DocumentId, request.DocumentType, cancellationToken);
    }
}
