using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterLotsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper, ILotCapacityCalculator capacityCalculator,
    ICodeGenerator codeGenerator, ILogger<RegisterLotsHandler> logger)
        : BaseLotsCapacityHandler<RegisterLotsCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(RegisterLotsCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚀Iniciando proceso de creación de tramos.");

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

        var existingLotsCount = await _unitOfWork.Lots.Entities
            .CountAsync(l => l.SectionId == section.Id && l.DeletedAt == null, cancellationToken);

        if (existingLotsCount + request.Quantity > LotsConstants.MaxLotsPerSection)
            return _errorManager.ThrowBadRequest<bool>(
                $"La sección solo permite un máximo de {LotsConstants.MaxLotsPerSection} tramos ({existingLotsCount} existentes + {request.Quantity} solicitados).",
                "ERP:SECTION_LOT_LIMIT_EXCEEDED");

        var (codesAreValid, codes) = await codeGenerator.GenerateUniqueStorageCodesAsync(
            StorageEntityType.Lot, section.Id, request.Quantity, cancellationToken);
        if (!codesAreValid)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene un código registrado para generar el código de los tramos.",
                "ERP:SECTION_CODE_NOT_FOUND");

        var lots = new List<Lots>(request.Quantity);
        var batchCapacities = new List<(decimal Width, decimal Length)>(request.Quantity);

        for (int i = 0; i < request.Quantity; i++)
        {
            var lot = LotsMapper.ToLotsEntity(_mapper, request, codes[i], section.Id);

            await _unitOfWork.Lots.RegisterLot(lot);

            for (int row = 1; row <= lot.NominalRows!.Value; row++)
            {
                for (int column = 1; column <= lot.NominalColumns!.Value; column++)
                {
                    var position = LotsMapper.ToLotsPositionEntity(
                        lot.Id,
                        codeGenerator.GeneratePositionCode(lot.Code, row, column),
                        row, column);

                    await _unitOfWork.LotsPositions.RegisterLotPosition(position);
                }
            }

            lots.Add(lot);
            batchCapacities.Add((request.Width, request.Length));
        }

        var calc = await capacityCalculator.CalculateLotsAsync(
            section.Id, batchCapacities, cancellationToken);

        if (calc.Lots.Count != lots.Count || calc.Section is null)
            return SectionCapacityNotFoundError();

        for (int i = 0; i < lots.Count; i++)
        {
            var lotCapacity = calc.Lots[i];
            lotCapacity.LotsId = lots[i].Id;
            await _unitOfWork.LotsCapacities.RegisterLotsCapacity(lotCapacity);
        }

        await ApplySectionCapacityAsync(section, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅Cración de tramos exitoso.");

        return true;
    }
}