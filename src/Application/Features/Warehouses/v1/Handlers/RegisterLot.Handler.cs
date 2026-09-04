using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterLotHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<RegisterLotCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(RegisterLotCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var sectionInfo = await _unitOfWork.Sections.Entities
            .Where(s => s.Id == request.SectionId && s.IsActive)
            .Select(s => new { s.SectionType, s.StorageType })
            .FirstOrDefaultAsync(cancellationToken);

        if (sectionInfo is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

        if (sectionInfo.SectionType == SectionType.Aisle)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden crear tramos en una sección de tipo pasillo.",
                "ERP:SECTION_TYPE_NOT_ALLOWED_FOR_LOTS");

        if (sectionInfo.StorageType == SectionStorageType.Racks)
            return _errorManager.ThrowBadRequest<bool>(
                "Esta sección está configurada para Racks. Crea una sección independiente para Tramos.",
                "ERP:SECTION_STORAGE_MISMATCH");

        if (sectionInfo.StorageType == SectionStorageType.Empty)
            return _errorManager.ThrowBadRequest<bool>(
                "Esta sección no admite tramos (StorageType Empty).",
                "ERP:SECTION_STORAGE_MISMATCH");

        var code = request.Code.Trim();
        var exists = await _unitOfWork.Lots.Entities
            .AnyAsync(l => l.SectionId == request.SectionId && l.Code == code, cancellationToken);

        if (exists)
            return _errorManager.ThrowBadRequest<bool>(
                $"Ya existe un tramo con el código '{code}' en la sección.",
                "ERP:LOT_CODE_ALREADY_EXISTS");

        var lot = request.ToLotEntity();
        await _unitOfWork.Lots.RegisterLot(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
