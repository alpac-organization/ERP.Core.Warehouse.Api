using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class DeleteSectionHandler(IUnitOfWork _unitOfWork, IErrorManager _erroManager, ILogger<DeleteSectionHandler> _logger) : BaseValidatorHandler<DeleteSectionCommand, bool>(_unitOfWork, _erroManager) 
   {
      public override async Task<bool> Handle(DeleteSectionCommand request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

         if (!access.IsSuccess)
         {
            return access.ErrorResponse!;
         }         

         _logger.LogInformation("🚩Iniciando proceso de eliminación de sección.");

         var section = await _unitOfWork.Sections.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.WarehouseId == request.WarehouseId, cancellationToken);

         if (section is null)
         {
            return _errorManager.ThrowBadRequest<bool>("No se encontró la sección a eliminar", "ERP:NOT_FOUND");
         }

         if (section.WarehouseId != request.WarehouseId)
         {
            return _errorManager.ThrowBadRequest<bool>("La sección no pertenece al almacén indicado.", "ERP:01");
         }

         section.IsActive = false;
         section.DeletedAt = DateTime.UtcNow;

         await _unitOfWork.Sections.UpdateAsync(section);
         await _unitOfWork.SaveChangesAsync(cancellationToken);

         _logger.LogInformation("Sección de almacén eliminada con éxito✅");

         return true;
      }
   }
}