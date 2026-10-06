using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetPositionsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetPositionsQuery, GetPositionsDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<GetPositionsDto> Handle(GetPositionsQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .Where(s =>
                s.Id == request.SectionId &&
                s.WarehouseId == request.WarehouseId &&
                s.DeletedAt == null &&
                s.IsActive)
            .Include(s => s.Lots.Where(lot =>
                lot.DeletedAt == null &&
                (!request.TramoId.HasValue || lot.Id == request.TramoId.Value)))
                .ThenInclude(lot => lot.Positions!.Where(position =>
                    position.DeletedAt == null &&
                    (!request.Status.HasValue || position.Status == request.Status.Value)))
                    .ThenInclude(position => position.LotsPositionsCoordinates)
            .Include(s => s.Racks.Where(rack =>
                rack.DeletedAt == null &&
                (!request.RackId.HasValue || rack.Id == request.RackId.Value)))
                .ThenInclude(rack => rack.Positions!.Where(position =>
                    position.DeletedAt == null &&
                    (!request.Status.HasValue || position.Status == request.Status.Value)))
                    .ThenInclude(position => position.RacksPositionsCoordinates)
            .FirstOrDefaultAsync(ct);

        if (section is null)
        {
            return _errorManager.ThrowBadRequest<GetPositionsDto>("La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");
        }

        if (section.SectionStorageType is not (SectionStorageType.Lots or SectionStorageType.Racks))
        {
            return _errorManager.ThrowBadRequest<GetPositionsDto>("La sección aún esta siendo codificada.", "ERP:POSITIONS_NOT_SUPPORTED");
        }

        return _mapper.Map<GetPositionsDto>(section);
    }
}
