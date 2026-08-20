using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class PermanentDeleteEvidenceHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IS3StorageService s3StorageService)
    : BaseValidatorHandler<PermanentDeleteEvidenceCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(PermanentDeleteEvidenceCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        // 1. Buscar el ReceptionEntrance para obtener las URLs
        var receptionEntrance = await _unitOfWork.ReceptionEntrance.Entities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RecordEntranceId == request.ReceptionId, cancellationToken);

        if (receptionEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        // 2. Guardar copia de las URLs originales
        var originalEvidenceUrls = receptionEntrance.EvidenceUrls?.ToList() ?? [];
        var originalDeletedEvidenceUrls = receptionEntrance.DeletedEvidenceUrls?.ToList() ?? [];

        var allUrls = originalEvidenceUrls.Concat(originalDeletedEvidenceUrls).Distinct().ToList();

        if (allUrls.Count == 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No hay imágenes de evidencia para eliminar.",
                "ERP:NO_EVIDENCE_FOUND");
        }

        try
        {
            // 3. Limpiar campos en BD PRIMERO
            receptionEntrance.EvidenceUrls = [];
            receptionEntrance.DeletedEvidenceUrls = [];

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Después de BD exitosa, eliminar imágenes de S3
            await s3StorageService.DeleteImagesAsync(allUrls, cancellationToken);

            return true;
        }
        catch (Exception)
        {
            // Si S3 falla, restaurar las URLs en BD
            receptionEntrance.EvidenceUrls = originalEvidenceUrls;
            receptionEntrance.DeletedEvidenceUrls = originalDeletedEvidenceUrls;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw;
        }
    }
}