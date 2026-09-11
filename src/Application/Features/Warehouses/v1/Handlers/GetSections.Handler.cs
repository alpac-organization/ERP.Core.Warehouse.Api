using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetSectionsHandler(IUnitOfWork _unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<GetSectionsQuery, PagedResponse<SectionDto>>(_unitOfWork, errorManager)
{
    public override async Task<PagedResponse<SectionDto>> Handle(GetSectionsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess) return access.ErrorResponse!;

        var sectionsQuery = _unitOfWork.Sections.Entities
            .AsNoTracking()
            .AsSplitQuery()
            .Where(s => s.WarehouseId == request.WarehouseId);

        sectionsQuery = ApplyFilters(sectionsQuery, request);

        var totalRecords = await sectionsQuery.CountAsync(cancellationToken);

        var sections = await sectionsQuery
            .OrderByDescending(sect => sect.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var sectionItems = mapper.Map<List<SectionDto>>(sections);

        return new PagedResponse<SectionDto>(
            sectionItems,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );
    }

    private static IQueryable<Sections> ApplyFilters(IQueryable<Sections> query, GetSectionsQuery request)
    {
        query = request.IsActive.HasValue
            ? query.Where(sect => sect.IsActive == request.IsActive.Value)
            : query.Where(sect => sect.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SectionCode))
            query = query.Where(sect => sect.Code == request.SectionCode);

        if (request.SectionType.HasValue)
            query = query.Where(sect => sect.SectionType == request.SectionType.Value);
        
        if (request.SectionStorageType.HasValue)
            query = query.Where(sect => sect.SectionStorageType == request.SectionStorageType.Value);

        return query;
    }
}
