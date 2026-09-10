using AutoMapper;
using Microsoft.Extensions.Logging;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterLotHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    ILotCapacityCalculator capacityCalculator,
    ILogger<RegisterLotHandler> logger)
    : BaseLotsCapacityHandler<RegisterLotCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(RegisterLotCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚀Iniciando proceso de registro de tramo.");

        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(request.UserId,
            request.CompanyId, request.ModuleCode, request.SectionId, request.WarehouseId, cancellationToken);
        if (!isValid) return errorResponse;

        if (section!.SectionType == SectionType.Aisle)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden crear tramos en una sección de tipo pasillo.",
                "ERP:SECTION_TYPE_NOT_ALLOWED_FOR_LOTS");

        if (section.SectionStorageType != SectionStorageType.Lots)
            return _errorManager.ThrowBadRequest<bool>(
                "Esta sección no admite tramos.",
                "ERP:SECTION_STORAGE_MISMATCH");

        request.Code = request.Code.Trim();

        var (codeIsValid, codeError) = await EnsureLotCodeAvailableAsync(
            request.Code, null, request.SectionId, cancellationToken);
        if (!codeIsValid)
            return codeError;

        var lot = _mapper.Map<Lots>(request);
        lot.Id = Guid.NewGuid();
        await _unitOfWork.Lots.RegisterLot(lot);

        var calc = await capacityCalculator.CalculateLotAsync(
            section.Id, request.WidthMetres, request.LengthMetres, cancellationToken);

        if (calc.Lot is null || calc.Section is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene capacidad registrada para recalcular.", "ERP:SECTION_CAPACITY_NOT_FOUND");

        var lotCapacity = calc.Lot;
        lotCapacity.LotsId = lot.Id;
        await _unitOfWork.LotsCapacities.RegisterLotsCapacity(lotCapacity);

        await ApplySectionCapacityAsync(section, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅Registro de tramo correctamente.");

        return true;
    }
}