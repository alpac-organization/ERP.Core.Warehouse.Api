using Microsoft.Extensions.Logging;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class RegisterSectionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ILogger<RegisterSectionHandler> logger)
        : BaseValidatorHandler<RegisterSectionCommand, bool>(unitOfWork, errorManager)
    {
        public override async Task<bool> Handle(RegisterSectionCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
            }

            logger.LogInformation("🚀Iniciando proceso de registro de sección.");

            #region Usuario actual
            var user = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
                return _errorManager.ThrowBadRequest<bool>(
                    "No se pudo identificar al usuario autenticado en el sistema.",
                    "ERP:USER_NOT_FOUND");

            var currentUserName = user.Fullname ?? user.UserName ?? request.UserId.ToString();
            #endregion

            var sectionEntity = request.ToSectionEntity(currentUserName);

            await _unitOfWork.Sections.RegisterSection(sectionEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("✅Registro de sección correctamente.");
            return true;
        }
    }
}