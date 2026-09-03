using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using MediatR;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetSectionByIdHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSectionByIdQuery, SectionDto>(unitOfWork, errorManager)
{
    public override async Task<SectionDto> Handle(
        GetSectionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            throw new UnauthorizedAccessException("Acceso denegado.");

        var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .AsSplitQuery()
            .Where(s => s.Id == request.SectionId && s.WarehouseId == request.WarehouseId)
            .Include(s => s.Racks).ThenInclude(r => r.Positions)
            .Include(s => s.Lots).ThenInclude(l => l.Positions)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Sección {request.SectionId} no encontrada.");

        var dto = mapper.Map<SectionDto>(section);

        var metrics = section.StorageType == SectionStorageType.Lots
            ? PositionMetrics.Summarize(
                section.Lots,
                lot => lot.Positions,
                p => p.IsOccupied || p.IsBlocked || p.IsReserved,
                lot => lot.WidthMetres,
                lot => lot.LengthMetres)
            : PositionMetrics.Summarize(
                section.Racks,
                rack => rack.Positions,
                p => p.IsOccupied || p.IsBlocked || p.IsReserved,
                rack => rack.WidthMetres,
                rack => rack.LengthMetres);

        dto.TotalAreaM2 = metrics.TotalAreaM2;
        dto.UsedAreaM2 = metrics.UsedAreaM2;
        dto.TotalPositions = metrics.TotalPositions;
        dto.UsedPositions = metrics.UsedPositions;

        return dto;
    }
}
