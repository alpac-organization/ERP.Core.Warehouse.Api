using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Application.Commons.Interfaces.Services.WarehouseCapacities;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterLotHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    ILotCapacityCalculator capacityCalculator)
    : BaseLotsCapacityHandler<RegisterLotCommand>(unitOfWork, errorManager, mapper)
{
    public override async Task<bool> Handle(RegisterLotCommand request, CancellationToken cancellationToken)
    {
        var (isValid, section, errorResponse) = await ValidateAccessAndGetSectionAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            request.SectionId,
            request.WarehouseId,
            cancellationToken);
        if (!isValid)
            return errorResponse;

        var lot = _mapper.Map<Lots>(request);
        lot.Id = Guid.NewGuid();
        lot.SectionId = section!.Id;
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

        return true;
    }
}