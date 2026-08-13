using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetRackSectionSummaryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetRackSectionSummaryQuery, RackSectionSummaryDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<RackSectionSummaryDto> Handle(GetRackSectionSummaryQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.IsActive, cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<RackSectionSummaryDto>(
                "La sección indicada no existe o no está activa.",
                "ERP:SECTION_NOT_FOUND");

        var racks = await _unitOfWork.Racks.Entities
            .Where(r => r.SectionId == request.SectionId)
            .ProjectTo<RackFlatDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var levelGroups = racks
            .GroupBy(r => r.LevelNumber)
            .OrderBy(g => g.Key)
            .ToList();

        var levelsCapacity = _mapper.Map<List<LevelCapacityDto>>(
            levelGroups,
            opts => opts.Items["SectionLength"] = section.LengthMetres);

        var statusGroups = racks
            .GroupBy(r => r.Status)
            .OrderByDescending(g => g.Count())
            .ToList();

        var statusBreakdown = _mapper.Map<List<RackStatusGroupDto>>(statusGroups);

        var usageProfileGroups = racks
            .GroupBy(r => r.UsageProfile)
            .OrderByDescending(g => g.Count())
            .ToList();

        var usageProfileBreakdown = _mapper.Map<List<RackUsageProfileGroupDto>>(usageProfileGroups);

        var dimensionGroups = racks
            .GroupBy(r => new RackDimensionKey(r.WidthMetres, r.LengthMetres, r.HeightMetres))
            .OrderByDescending(g => g.Count())
            .ToList();

        var dimensionGroupsDto = _mapper.Map<List<RackDimensionGroupDto>>(dimensionGroups);

        return new RackSectionSummaryDto
        {
            SectionId = request.SectionId,
            SectionLengthMetres = section.LengthMetres,
            TotalRacksCount = racks.Count,
            LevelsCapacity = levelsCapacity,
            StatusBreakdown = statusBreakdown,
            UsageProfileBreakdown = usageProfileBreakdown,
            DimensionGroups = dimensionGroupsDto
        };
    }
}