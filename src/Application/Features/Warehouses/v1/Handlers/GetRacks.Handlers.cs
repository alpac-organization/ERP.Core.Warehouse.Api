using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

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

        var levelsCapacity = racks
            .GroupBy(r => r.LevelNumber)
            .Select(g => new LevelCapacityDto
            {
                LevelNumber = g.Key,
                RacksCount = g.Count(),
                UsedLengthMetres = g.Sum(r => r.LengthMetres),
                AvailableLengthMetres = section.LengthMetres - g.Sum(r => r.LengthMetres)
            })
            .OrderBy(x => x.LevelNumber)
            .ToList();

        var statusBreakdown = racks
            .GroupBy(r => r.Status)
            .Select(g => new RackStatusGroupDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var usageProfileBreakdown = racks
            .GroupBy(r => r.UsageProfile)
            .Select(g => new RackUsageProfileGroupDto
            {
                UsageProfile = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var dimensionGroups = racks
            .GroupBy(r => new { r.WidthMetres, r.LengthMetres, r.HeightMetres })
            .Select(g => new RackDimensionGroupDto
            {
                WidthMetres = g.Key.WidthMetres,
                LengthMetres = g.Key.LengthMetres,
                HeightMetres = g.Key.HeightMetres,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new RackSectionSummaryDto
        {
            SectionId = request.SectionId,
            SectionLengthMetres = section.LengthMetres,
            TotalRacksCount = racks.Count,
            LevelsCapacity = levelsCapacity,
            StatusBreakdown = statusBreakdown,
            UsageProfileBreakdown = usageProfileBreakdown,
            DimensionGroups = dimensionGroups
        };
    }
}