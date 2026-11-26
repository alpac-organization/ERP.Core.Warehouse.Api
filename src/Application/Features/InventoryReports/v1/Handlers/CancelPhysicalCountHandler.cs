using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers
{
    public class CancelPhysicalCountHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
        : BaseValidatorHandler<CancelPhysicalCountCommand, bool>(unitOfWork, errorManager)
    {
        public override async Task<bool> Handle(CancelPhysicalCountCommand request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
            if (!access.IsSuccess)
                return access.ErrorResponse!;

            if (access.Role?.RoleType != RoleType.Administrator)
            {
                return _errorManager.ThrowForbidden<bool>(
                    "No tiene permisos para eliminar levantamientos físicos.",
                    "ERP:FORBIDDEN");
            }

            var physicalCount = await _unitOfWork.PhysicalCounts.Entities
                .FirstOrDefaultAsync(
                    pc => pc.Id == request.PhysicalCountId && pc.CompanyId == request.CompanyId,
                    ct);

            if (physicalCount == null)
            {
                return _errorManager.ThrowBadRequest<bool>("Levantamiento fisico no encontrado.", "ERP:007");
            }

            if (physicalCount.BranchId != access.Profile.BranchId)
            {
                return _errorManager.ThrowUnauthorized<bool>(
                    "No tiene acceso a levantamientos fisicos de otra sucursal.",
                    "ERP:INVALID_BRANCH_ACCESS");
            }

            if (physicalCount.Status == PhysicalCountStatus.Completed)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "No se puede eliminar un levantamiento completado.",
                    "ERP:PHYSICAL_COUNT_COMPLETED");
            }

            var lines = await _unitOfWork.PhysicalCountLines.Entities
                .Where(l => l.PhysicalCountId == physicalCount.Id)
                .ToListAsync(ct);

            if (lines.Count > 0)
            {
                await _unitOfWork.PhysicalCountLines.RemoveRangeAsync(lines);
            }

            await _unitOfWork.PhysicalCounts.RemoveRangeAsync([physicalCount]);
            await _unitOfWork.SaveChangesAsync(ct);

            return true;
        }
    }
}
