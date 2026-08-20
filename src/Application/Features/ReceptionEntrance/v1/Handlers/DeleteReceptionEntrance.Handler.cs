using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class DeleteReceptionEntranceHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IS3StorageService s3StorageService)
    : BaseValidatorHandler<DeleteReceptionEntranceCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(DeleteReceptionEntranceCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        // 1. Buscar el registro completo con todas sus relaciones
        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance)
            .Include(r => r.EntranceDucats.Where(d => d.DeletedAt == null))
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(c => c.Details)
            .Include(r => r.ExecutionLogs)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        // 2. Verificar que no haya avanzado de paso
        if (recordEntrance.CurrentStepCode != WorkflowStepCodes.Reception)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"No es posible eliminar este expediente porque ya avanzó en el ciclo.",
                "ERP:RECORD_ALREADY_ADVANCED");
        }

        // 3. Obtener las URLs activas para mover a papelera
        var activeEvidenceUrls = recordEntrance.ReceptionEntrance?.EvidenceUrls ?? [];

        var urlsToMove = activeEvidenceUrls
            .Where(url => url.Contains($"/{S3Sections.ReceptionEvidence}/"))
            .Distinct()
            .ToList();

        try
        {
            // 4. Soft delete de ReceptionEntrance
            if (recordEntrance.ReceptionEntrance != null)
            {
                var deletedUrls = recordEntrance.ReceptionEntrance.DeletedEvidenceUrls ?? [];
                deletedUrls.AddRange(activeEvidenceUrls);
                recordEntrance.ReceptionEntrance.DeletedEvidenceUrls = deletedUrls.Distinct().ToList();
                recordEntrance.ReceptionEntrance.EvidenceUrls = [];
                recordEntrance.ReceptionEntrance.DeletedAt = NicaraguaClock.Now;
            }

            // ... resto de soft deletes ...

            // 9. Guardar en BD PRIMERO
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 10. Después de BD exitosa, mover imágenes a papelera de S3
            if (urlsToMove.Count > 0)
            {
                var movedUrls = (await s3StorageService.MoveImagesAsync(
                    sourceUrls: urlsToMove,
                    Module: S3Sections.Module,
                    sourceSection: S3Sections.ReceptionEvidence,
                    destinationSection: S3Sections.ReceptionEvidenceDeleted,
                    cancellationToken: cancellationToken)).ToList();

                // 11. ACTUALIZAR las URLs en BD con las nuevas URLs de papelera
                if (movedUrls.Count > 0 && recordEntrance.ReceptionEntrance != null)
                {
                    var updatedDeletedUrls = recordEntrance.ReceptionEntrance.DeletedEvidenceUrls ?? [];

                    // Reemplazar URLs originales por las movidas
                    updatedDeletedUrls.RemoveAll(url => urlsToMove.Contains(url));
                    updatedDeletedUrls.AddRange(movedUrls);

                    recordEntrance.ReceptionEntrance.DeletedEvidenceUrls = updatedDeletedUrls.Distinct().ToList();

                    // Guardar la actualización de URLs
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }

        return true;
    }
}