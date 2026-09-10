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

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
    public class RegisterSectionHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper, ISectionCapacityCalculator _sectionCapacityCalculator, ILogger<RegisterSectionHandler> logger) : BaseValidatorHandler<RegisterSectionCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(RegisterSectionCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess) return access.ErrorResponse;

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:01");
            }

            logger.LogInformation("🚀Iniciando proceso de registro de sección.");

            var section = SectionMapper.ToSectionEntity(request);
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

            await _unitOfWork.Sections.RegisterSection(section);
            await _unitOfWork.SectionCapacities.RegisterSectionCapacity(sectionCapacity);
            await _unitOfWork.WarehouseCapacities.UpdateAsync(warehouseCapacity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("✅Registro de sección correctamente.");

            return true;
        }
    }
}