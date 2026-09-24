using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetRackDetailsHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper)
    : BaseValidatorHandler<GetRackDetailsQuery, RackDetailsDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<RackDetailsDto> Handle(GetRackDetailsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.WarehouseId == request.WarehouseId && s.DeletedAt == null, cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<RackDetailsDto>(
                "La sección no fue encontrada o no pertenece al almacén indicado.",
                "ERP:SECTION_NOT_FOUND");

        var rack = await _unitOfWork.Racks.Entities
            .AsNoTracking()
            .Include(r => r.RackCapacity)
            .Include(r => r.RacksCoordinates)
            .Include(r => r.Positions.Where(p => p.DeletedAt == null))
                .ThenInclude(p => p.StockPlacements.Where(sp => sp.VacatedAtDate == null && sp.DeletedAt == null))
                    .ThenInclude(sp => sp.Stock)
                        .ThenInclude(st => st.Product)
            .FirstOrDefaultAsync(r => r.Id == request.RackId && r.SectionId == request.SectionId && r.DeletedAt == null, cancellationToken);

        if (rack is null)
            return _errorManager.ThrowBadRequest<RackDetailsDto>(
                "El rack solicitado no existe.",
                "ERP:RACK_NOT_FOUND");

        var dto = _mapper.Map<RackDetailsDto>(rack);

        // Mapear stock activo en cada posición si existe
        if (rack.Positions != null)
        {
            foreach (var pos in rack.Positions)
            {
                var targetPosDto = dto.Positions.FirstOrDefault(p => p.PositionId == pos.Id);
                if (targetPosDto == null) continue;

                var activePlacement = pos.StockPlacements.FirstOrDefault(sp => sp.VacatedAtDate == null);
                if (activePlacement?.Stock != null)
                {
                    targetPosDto.CurrentStock = new StockPlacementSummaryDto
                    {
                        StockId = activePlacement.Stock.Id,
                        ProductName = activePlacement.Stock.Product?.Name,
                        CategoryName = activePlacement.Stock.Product?.Code,
                        CurrentWeightKg = activePlacement.Stock.CurrentWeightKg,
                        CurrentBultos = activePlacement.Stock.CurrentBultos,
                        PlacedAtDate = activePlacement.PlacedAtDate,
                        PlacedAtTime = activePlacement.PlacedAtTime
                    };
                }
            }
        }

        return dto;
    }
}
