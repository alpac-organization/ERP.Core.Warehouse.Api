using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetSectionsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSectionsQuery, PagedResponse<SectionDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<SectionDto>> Handle(
        GetSectionsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var sectionsQuery = _unitOfWork.Sections.Entities
            .AsNoTracking()
            .AsSplitQuery()
            .Where(s => s.WarehouseId == request.WarehouseId);

        sectionsQuery = ApplyFilters(sectionsQuery, request);

        var totalRecords = await sectionsQuery.CountAsync(cancellationToken);

        var sections = await sectionsQuery
            .OrderBy(sect => sect.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(section => section.Racks)
                .ThenInclude(rack => rack.Positions)
            .Include(section => section.Lots)
                .ThenInclude(lot => lot.Positions)
            .ToListAsync(cancellationToken);

        var sectionItems = mapper.Map<List<SectionDto>>(sections);

        foreach (var (section, sectionItem) in sections.Zip(sectionItems))
        {
            var metrics = section.StorageType == ERP.Core.Database.Domain.Enums.SectionStorageType.Lots
                ? PositionMetrics.Summarize(
                    section.Lots,
                    lot => lot.Positions,
                    position => position.IsOccupied || position.IsBlocked || position.IsReserved,
                    lot => lot.WidthMetres,
                    lot => lot.LengthMetres)
                : PositionMetrics.Summarize(
                    section.Racks,
                    rack => rack.Positions,
                    position => position.IsOccupied || position.IsBlocked || position.IsReserved,
                    rack => rack.WidthMetres,
                    rack => rack.LengthMetres);

            sectionItem.TotalAreaM2 = metrics.TotalAreaM2;
            sectionItem.UsedAreaM2 = metrics.UsedAreaM2;
            sectionItem.TotalPositions = metrics.TotalPositions;
            sectionItem.UsedPositions = metrics.UsedPositions;

        }

        return new PagedResponse<SectionDto>(
            sectionItems,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }

    private static IQueryable<Sections> ApplyFilters(
        IQueryable<Sections> query,
        GetSectionsQuery request)
    {
        query = request.IsActive.HasValue
            ? query.Where(sect => sect.IsActive == request.IsActive.Value)
            : query.Where(sect => sect.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SectionCode))
            query = query.Where(sect => sect.Code == request.SectionCode);

        if (request.SectionType.HasValue)
            query = query.Where(sect => sect.SectionType == request.SectionType.Value);

        if (request.SectionStorageType.HasValue)
            query = query.Where(sect => sect.StorageType == request.SectionStorageType.Value);

        return query;
    }
}
