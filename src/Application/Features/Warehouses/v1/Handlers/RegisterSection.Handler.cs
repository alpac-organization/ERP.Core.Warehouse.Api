using Microsoft.Extensions.Logging;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ERP.Core.Database.Application.Commons.Interfaces.Services;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class RegisterSectionHandler(
        IUnitOfWork _unitOfWork,
        IErrorManager _errorManager, IMapper _mapper,
        ICodeGenerator _codeGenerator, ISectionCapacityCalculator _sectionCapacityCalculator,
        ILogger<RegisterSectionHandler> _logger) : BaseValidatorHandler<RegisterSectionCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(RegisterSectionCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess) return access.ErrorResponse;

            if (access.Role?.RoleType != RoleType.Administrator)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
            }

            var warehouseExists = await _unitOfWork.Warehouses.Entities
                .AnyAsync(w => w.Id == request.WarehouseId && w.IsActive, cancellationToken);

            if (!warehouseExists)
            {
                return _errorManager.ThrowBadRequest<bool>("El almacén indicado no existe o no está activo.", "ERP:01");
            }

            if (request.SectionType == SectionType.Storage &&
                request.SectionStorageType is not (SectionStorageType.Racks or SectionStorageType.Lots))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Una sección de almacenamiento solo admite Racks o Lots.",
                    "ERP:SECTION_STORAGE_MISMATCH");
            }

            if (request.SectionType == SectionType.Aisle &&
                request.SectionStorageType is not (SectionStorageType.Pallets or SectionStorageType.None))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Una sección de tipo pasillo solo admite almacenamiento en polines o sin almacenamiento.",
                    "ERP:SECTION_STORAGE_MISMATCH");
            }

            if (request.SectionType != SectionType.Aisle && request.MaximumNumberOfPalletsPerLevel.HasValue)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El número máximo de polines por nivel solo es para tipo pasillo (Aisle).",
                    "ERP:SECTION_STORAGE_MISMATCH");
            }

            if (request.SectionType == SectionType.Aisle &&
                request.SectionStorageType == SectionStorageType.Pallets &&
                (!request.MaximumNumberOfPalletsPerLevel.HasValue || request.MaximumNumberOfPalletsPerLevel <= 0))
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Si el pasillo permite almacenamiento (Polines), el número máximo de polines por nivel debe ser mayor a cero.",
                    "ERP:SECTION_STORAGE_MISMATCH");
            }

            if (request.SectionType == SectionType.Aisle &&
                request.SectionStorageType == SectionStorageType.None &&
                request.MaximumNumberOfPalletsPerLevel.HasValue)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "El número máximo de polines por nivel solo aplica cuando el pasillo permite almacenamiento (Pallets).",
                    "ERP:SECTION_STORAGE_MISMATCH");
            }

            var (isCodeGenerated, sectionCode) = await _codeGenerator.GenerateUniqueSectionCodeAsync(
                request.WarehouseId,
                request.SectionType,
                request.SectionStorageType,
                cancellationToken);

            if (!isCodeGenerated)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "No se pudo generar el código de la sección. Verifica el tipo de sección y el tipo de almacenamiento.",
                    "ERP:SECTION_CODE_GENERATION_FAILED");
            }

            _logger.LogInformation("🚀Iniciando proceso de registro de sección.");

            var section = SectionMapper.ToSectionEntity(request, sectionCode);

            var capacityCalculation = await _sectionCapacityCalculator.CalculateSectionAsync(
                request.WarehouseId,
                request.Width, request.Length,
                request.SectionType, cancellationToken
            );

            if (capacityCalculation.Section is null || capacityCalculation.Warehouse is null)
            {
                return _errorManager.ThrowBadRequest<bool>("No se pudo calcular la capacidad de la sección.", "ERP:01");
            }

            var sectionCapacity = SectionMapper.ToSectionCapacityEntity(request, section.Id, capacityCalculation.Section);

            var warehouseCapacity = await _unitOfWork.WarehouseCapacities.Entities
                .FirstOrDefaultAsync(c => c.WarehouseId == request.WarehouseId, cancellationToken);

            if (warehouseCapacity is null)
            {
                return _errorManager.ThrowBadRequest<bool>("El almacén no tiene capacidad registrada", "ERP:01");
            }

            _mapper.Map(capacityCalculation.Warehouse, warehouseCapacity);

            var sectionCoordinates = request.ToSectionCoordinateEntity(section.Id);

            await _unitOfWork.Sections.RegisterSection(section);
            await _unitOfWork.SectionCapacities.RegisterSectionCapacity(sectionCapacity);
            await _unitOfWork.SectionCoordinates.RegisterSectionCoordinates(sectionCoordinates);
            await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouseCapacity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅Registro de sección correctamente.");

            return true;
        }
    }
}