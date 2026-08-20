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

        // 3. Mover solo las imágenes ACTIVAS a la papelera de S3
        var activeEvidenceUrls = recordEntrance.ReceptionEntrance?.EvidenceUrls ?? [];

        var urlsToMove = activeEvidenceUrls
            .Where(url => url.Contains($"/{S3Sections.ReceptionEvidence}/"))
            .Distinct()
            .ToList();

        if (urlsToMove.Count > 0)
        {
            await s3StorageService.MoveImagesAsync(
                sourceUrls: urlsToMove,
                Module: S3Sections.Module,
                sourceSection: S3Sections.ReceptionEvidence,
                destinationSection: S3Sections.ReceptionEvidenceDeleted,
                cancellationToken: cancellationToken);
        }

        // 4. Soft delete de ReceptionEntrance
        if (recordEntrance.ReceptionEntrance != null)
        {
            // Mover las URLs activas al historial de eliminadas
            var deletedUrls = recordEntrance.ReceptionEntrance.DeletedEvidenceUrls ?? [];
            deletedUrls.AddRange(activeEvidenceUrls);
            recordEntrance.ReceptionEntrance.DeletedEvidenceUrls = deletedUrls.Distinct().ToList();

            // Vaciar solo las activas
            recordEntrance.ReceptionEntrance.EvidenceUrls = [];

            recordEntrance.ReceptionEntrance.DeletedAt = NicaraguaClock.Now;
        }

        // 5. Soft delete de EntranceDucats
        foreach (var ducat in recordEntrance.EntranceDucats)
        {
            ducat.DeletedAt = NicaraguaClock.Now;
        }

        // 6. Soft delete de CustomsDeclarations y sus Details
        if (recordEntrance.CustomsDeclarations != null)
        {
            recordEntrance.CustomsDeclarations.DeletedAt = NicaraguaClock.Now;

            if (recordEntrance.CustomsDeclarations.Details != null)
            {
                recordEntrance.CustomsDeclarations.Details.DeletedAt = NicaraguaClock.Now;
            }
        }

        // 7. Soft delete de StepExecutionLogs
        foreach (var log in recordEntrance.ExecutionLogs)
        {
            log.DeletedAt = NicaraguaClock.Now;
        }

        // 8. Soft delete de RecordEntrance
        recordEntrance.DeletedAt = NicaraguaClock.Now;

        // 9. Guardar cambios
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}