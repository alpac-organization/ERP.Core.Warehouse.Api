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

public class GetSectionsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetSectionsQuery, PagedResponse<SectionDto>>(_unitOfWork, _errorManager)
{
    public override async Task<PagedResponse<SectionDto>> Handle(GetSectionsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess) return access.ErrorResponse!;

        var warehouse = await _unitOfWork.Warehouses.Entities
            .AsNoTracking().FirstOrDefaultAsync(w =>
                w.Id == request.WarehouseId &&
                w.DeletedAt == null &&
                w.IsActive, cancellationToken);

        if (warehouse is null)
        {
            return _errorManager.ThrowBadRequest<PagedResponse<SectionDto>>("El almacén indicado no existe o no está activo.", "ERP:01");
        }

        var sectionsQuery = _unitOfWork.Sections.Entities
            .AsNoTracking()
            .Where(s => s.WarehouseId == request.WarehouseId && s.DeletedAt == null);

        sectionsQuery = ApplyFilters(sectionsQuery, request);

        var totalRecords = await sectionsQuery.CountAsync(cancellationToken);

        var sections = await sectionsQuery
            .OrderByDescending(sect => sect.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var sectionItems = _mapper.Map<List<SectionDto>>(sections);

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
