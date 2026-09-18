using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class RegisterSectionCoordinateHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<RegisterSectionCoordinateHandler> _logger) : BaseValidatorHandler<RegisterSectionCoordinateCommand, bool>(_unitOfWork, _errorManager)
   {
      public override async Task<bool> Handle(RegisterSectionCoordinateCommand request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

         if (!access.IsSuccess) return access.ErrorResponse;

         _logger.LogInformation("🚀Iniciando proceso de registro de coordenadas de sección.");

         var section = await _unitOfWork.Sections.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.DeletedAt == null && s.IsActive, cancellationToken);

         if (section is null)
         {
            return _errorManager.ThrowBadRequest<bool>("La sección indicada no existe o no está activa.", "ERP:01");
         }

         if (section.WarehouseId != request.WarehouseId)
         {
            return _errorManager.ThrowBadRequest<bool>("La sección no pertenece al almacén indicado.", "ERP:01");
         }

         var sectionCoordinates = request.ToSectionCoordinateEntity(request.SectionId);

         await _unitOfWork.SectionCoordinates.RegisterSectionCoordinates(sectionCoordinates);
         await _unitOfWork.SaveChangesAsync(cancellationToken);

         _logger.LogInformation("✅Registro de coordenadas de sección correctamente.");

         return true;
      }
   }
}
