using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class UpdateLotHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    AutoMapper.IMapper mapper,
    ILotCapacityCalculator capacityCalculator,
    ILogger<UpdateLotHandler> logger)
    : BaseLotsCapacityHandler<UpdateLotCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(UpdateLotCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚀Iniciando proceso de actualización de tramo.");

        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            request.SectionId,
            request.WarehouseId,
            cancellationToken);
        if (!isValid)
            return errorResponse;

        var lot = await _unitOfWork.Lots.Entities
            .Include(l => l.LotsCapacity)
            .FirstOrDefaultAsync(
                l => l.Id == request.LotId
                    && l.SectionId == request.SectionId
                    && l.DeletedAt == null,
                cancellationToken);
        if (lot is null)
            return _errorManager.ThrowBadRequest<bool>(
                "El tramo no fue encontrado.", "ERP:LOT_NOT_FOUND");

        if (request.Code != null)
        {
            request.Code = request.Code.Trim();

            if (request.Code != lot.Code)
            {
                var codeExists = await _unitOfWork.Lots.Entities
                    .AnyAsync(
                        l => l.SectionId == request.SectionId
                            && l.Code == request.Code
                            && l.DeletedAt == null,
                        cancellationToken);

                if (codeExists)
                    return _errorManager.ThrowBadRequest<bool>(
                        $"Ya existe un tramo con el código '{request.Code}' en la sección.",
                        "ERP:LOT_CODE_ALREADY_EXISTS");
            }
        }

        if (request.Code != null) lot.Code = request.Code;
        if (request.AllowsStacking.HasValue) lot.AllowsStacking = request.AllowsStacking.Value;
        if (request.Status.HasValue)
        {
            if (request.Status.Value != lot.Status)
                lot.StatusChangedAt = DateTime.UtcNow;
            lot.Status = request.Status.Value;
        }
        if (request.UnavailableReason != null) lot.UnavailableReason = request.UnavailableReason;

        var calc = await capacityCalculator.UpdateLotAsync(
            lot.Id, request.WidthMetres, request.LengthMetres, cancellationToken);

        if (calc.Lot is null || calc.Section is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección no tiene capacidad registrada para recalcular.", "ERP:SECTION_CAPACITY_NOT_FOUND");

        if (lot.LotsCapacity is null)
        {
            calc.Lot.LotsId = lot.Id;
            await _unitOfWork.LotsCapacities.RegisterLotsCapacity(calc.Lot);
        }
        else
        {
            _mapper.Map(calc.Lot, lot.LotsCapacity);
            await _unitOfWork.LotsCapacities.UpdateAsync(lot.LotsCapacity);
        }

        await ApplySectionCapacityAsync(section!, calc.Section, cancellationToken);
        await ApplyWarehouseCapacityAsync(section!.WarehouseId, calc.Warehouse, cancellationToken);

        await _unitOfWork.Lots.UpdateAsync(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅Tramo actualizado correctamente.");

        return true;
    }
}