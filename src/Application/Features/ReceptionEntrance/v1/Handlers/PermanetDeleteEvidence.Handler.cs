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

        // 1. Buscar el ReceptionEntrance para obtener las URLs (incluyendo soft deleted)
        var receptionEntrance = await _unitOfWork.ReceptionEntrance.Entities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RecordEntranceId == request.ReceptionId, cancellationToken);

        if (receptionEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        // 2. Obtener todas las URLs de evidencia (activas + eliminadas)
        var allUrls = new List<string>();

        if (receptionEntrance.EvidenceUrls != null)
        {
            allUrls.AddRange(receptionEntrance.EvidenceUrls);
        }

        if (receptionEntrance.DeletedEvidenceUrls != null)
        {
            allUrls.AddRange(receptionEntrance.DeletedEvidenceUrls);
        }

        // 3. Verificar que haya imágenes para eliminar
        if (allUrls.Count == 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No hay imágenes de evidencia para eliminar.",
                "ERP:NO_EVIDENCE_FOUND");
        }

        // 4. Eliminar imágenes de S3 (definitivo)
        await s3StorageService.DeleteImagesAsync(allUrls.Distinct(), cancellationToken);

        // 5. Limpiar los campos de la BD
        receptionEntrance.EvidenceUrls = [];
        receptionEntrance.DeletedEvidenceUrls = [];

        // 6. Guardar cambios
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}