using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using AutoMapper.QueryableExtensions;
using AutoMapper;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetRacksBySectionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetRacksBySectionQuery, RackSectionFilterResultDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<RackSectionFilterResultDto> Handle(GetRacksBySectionQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.IsActive, cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<RackSectionFilterResultDto>(
                "La sección indicada no existe o no está activa.",
                "ERP:SECTION_NOT_FOUND");

        var query = _unitOfWork.Racks.Entities
            .Where(r => r.SectionId == request.SectionId);

        // Aplicar filtros (igual que antes)
        if (request.LevelNumber.HasValue)
            query = query.Where(r => r.LevelNumber == request.LevelNumber.Value);
        if (request.Status.HasValue)
            query = query.Where(r => r.Status == request.Status.Value);
        if (request.UsageProfile.HasValue)
            query = query.Where(r => r.UsageProfile == request.UsageProfile.Value);
        if (request.WidthMetres.HasValue)
            query = query.Where(r => r.WidthMetres == request.WidthMetres.Value);
        if (request.LengthMetres.HasValue)
            query = query.Where(r => r.LengthMetres == request.LengthMetres.Value);
        if (request.HeightMetres.HasValue)
            query = query.Where(r => r.HeightMetres == request.HeightMetres.Value);

        var racks = await query
            .OrderBy(r => r.LevelNumber)
            .ThenBy(r => r.RowNumber)
            .ProjectTo<RackListDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new RackSectionFilterResultDto
        {
            SectionId = request.SectionId,
            TotalRacksCount = racks.Count,
            Racks = racks
        };
    }
}

public class GetRackByIdHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetRackByIdQuery, RackDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<RackDto> Handle(GetRackByIdQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var rack = await _unitOfWork.Racks.Entities
            .Where(r => r.Id == request.RackId)
            .ProjectTo<RackDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (rack is null)
            return _errorManager.ThrowBadRequest<RackDto>(
                "El rack indicado no existe.",
                "ERP:RACK_NOT_FOUND");

        return rack;
    }
}