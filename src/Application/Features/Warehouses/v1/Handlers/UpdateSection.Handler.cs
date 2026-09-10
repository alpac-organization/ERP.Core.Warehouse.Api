using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class UpdateSectionHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper, ISectionCapacityCalculator _sectionCapacityCalculator, ILogger<UpdateSectionHandler> _logger) : BaseValidatorHandler<UpdateSectionCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(UpdateSectionCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess) return access.ErrorResponse;

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
            }

            _logger.LogInformation("🚩Iniciando actualización de sección.");

            var section = await _unitOfWork.Sections.Entities
                .Include(s => s.SectionCapacity)
                .FirstOrDefaultAsync(s =>
                    s.Id == request.SectionId &&
                    s.WarehouseId == request.WarehouseId &&
                    s.IsActive &&
                    s.DeletedAt == null,
                    cancellationToken);

            if (section is null)
            {
                return _errorManager.ThrowBadRequest<bool>("La sección indicada no existe o no pertenece al almacén.", "ERP:01");
            }

            var warehouseExists = await _unitOfWork.Warehouses.Entities
                .AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, cancellationToken);

            if (!warehouseExists)
            {
                return _errorManager.ThrowBadRequest<bool>("El almacén indicado no existe o no está activo.", "ERP:01");
            }

            if (request.Code is not null)
            {
                var codeExists = await _unitOfWork.Sections.Entities
                    .AnyAsync(s =>
                        s.WarehouseId == request.WarehouseId &&
                        s.Code == request.Code &&
                        s.Id != request.SectionId, cancellationToken);

                if (codeExists)
                {
                    return _errorManager.ThrowBadRequest<bool>("Ya existe una sección con ese código en el almacén.", "ERP:01");
                }
            }

            var shouldRecalculateCapacity = request.Width.HasValue || request.Length.HasValue;


            // var sectionType = request.SectionType ?? section.SectionType;

            if (shouldRecalculateCapacity)
            {
                var width = request.Width ?? section.SectionCapacity?.Width;
                var length = request.Length ?? section.SectionCapacity?.Length;

                if (!width.HasValue || !length.HasValue)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        "No se puede recalcular la capacidad porque la sección no tiene ancho y largo registrados.", "ERP:01");
                }

                var sectionCapacityCalculation = await _sectionCapacityCalculator.UpdateSectionAsync(
                    request.SectionId,
                    width.Value,
                    length.Value,
                    cancellationToken
                );

                if (sectionCapacityCalculation.Section is null || sectionCapacityCalculation.Warehouse is null)
                {
                    return _errorManager.ThrowBadRequest<bool>("No se pudo calcular la capacidad de la sección.", "ERP:01");
                }

                var warehouseCapacity = await _unitOfWork.WarehouseCapacities.Entities
                    .FirstOrDefaultAsync(c => c.WarehouseId == request.WarehouseId, cancellationToken);

                if (warehouseCapacity is null)
                {
                    return _errorManager.ThrowBadRequest<bool>("El almacén no tiene capacidad registrada", "ERP:01");
                }

                if (section.SectionCapacity is not null)
                {
                    await _unitOfWork.SectionCapacities.UpdateAsync(section.SectionCapacity);
                }

                _mapper.Map(sectionCapacityCalculation.Warehouse, warehouseCapacity);
                await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouseCapacity);
            }

            await _unitOfWork.Sections.UpdateAsync(section);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Sección de Almacén actualizada con éxito✅");

            return true;
        }
    }
}
