using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetSectionsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSectionsQuery, PagedResponse<SectionDto>>(unitOfWork, errorManager)
{

    public override async Task<PagedResponse<SectionDto>> Handle(GetSectionsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;


        var sectionsQuery = _unitOfWork.Sections.Entities
            .AsNoTracking()
            .Where(s => s.WarehouseId == request.WarehouseId);

        sectionsQuery = ApplyFilters(sectionsQuery, request);

        var totalRecords = await sectionsQuery.CountAsync(cancellationToken);

        var sections = sectionsQuery
                       .OrderBy(sect => sect.Code)
                       .Skip((request.PageNumber - 1) * request.PageSize)
                       .Take(request.PageSize)
                       .ToListAsync(cancellationToken);




        return new PagedResponse<SectionDto>(
            mapper.Map<List<SectionDto>>(sections),
            request.PageSize,
            request.PageNumber,
            totalRecords
        );
    }
    private static IQueryable<Sections> ApplyFilters(IQueryable<Sections> query, GetSectionsQuery request)
    {
        query = request.IsActive.HasValue
                ? query.Where(sect => sect.IsActive == request.IsActive.Value)
                : query.Where(sect => sect.IsActive);

        if (!string.IsNullOrEmpty(request.SectionCode))
        {
            query = query.Where(sect => sect.Code == request.SectionCode);
        }
        if (request.SectionType.HasValue)
        {
            query = query.Where(sect => sect.SectionType == request.SectionType);
        }
        if (request.SectionStorageType.HasValue)
        {
            query = query.Where(sect => sect.StorageType == request.SectionStorageType.Value);
        }
        return query;
    }
}