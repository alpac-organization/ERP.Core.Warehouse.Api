using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using AutoMapper.QueryableExtensions;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetLotByIdHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetLotByIdQuery, LotDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<LotDto> Handle(
        GetLotByIdQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var lot = await _unitOfWork.Lots.Entities
            .AsNoTracking()
            .Include(l => l.Positions)
            .FirstOrDefaultAsync(
                l => l.Id == request.LotId
                    && l.SectionId == request.SectionId
                    && l.DeletedAt == null,
                cancellationToken);

        if (lot is null)
            return _errorManager.ThrowNotFound<LotDto>(
                "El tramo no fue encontrado.",
                "ERP:LOT_NOT_FOUND");

        return _mapper.Map<LotDto>(lot);
    }
}

#region Get Lots por Section
public class GetLotsBySectionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetLotsBySectionQuery, PagedResponse<LotListItemDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<LotListItemDto>> Handle(
    GetLotsBySectionQuery request,
    CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var queryLots = _unitOfWork.Lots.Entities
            .AsNoTracking()
            .Where(lot => lot.DeletedAt == null && lot.SectionId == request.SectionId);

        // Aplicar filtros (por código y estado)
        if (!string.IsNullOrWhiteSpace(request.Code))
            queryLots = queryLots.Where(lot => lot.Code == request.Code);
        if (request.RackStatus.HasValue)
            queryLots = queryLots.Where(lot => lot.Status == request.RackStatus.Value);

        var totalRecords = await queryLots.CountAsync(cancellationToken);

        var lots = await queryLots
            .OrderBy(lot => lot.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<LotListItemDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new PagedResponse<LotListItemDto>(
            lots,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }
}
#endregion
