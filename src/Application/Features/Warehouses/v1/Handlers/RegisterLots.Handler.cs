using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterLotsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<RegisterLotsCommand, RegisterLotsResultDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<RegisterLotsResultDto> Handle(RegisterLotsCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.IsActive, cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<RegisterLotsResultDto>(
                "La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

        if (section.SectionType == SectionType.Aisle)
            return _errorManager.ThrowBadRequest<RegisterLotsResultDto>(
                "No se pueden crear tramos en una sección de tipo pasillo.",
                "ERP:SECTION_TYPE_NOT_ALLOWED_FOR_LOTS");

        var sectionHasRacks = await _unitOfWork.Racks.Entities
            .AnyAsync(r => r.SectionId == request.SectionId, cancellationToken);

        if (sectionHasRacks)
            return _errorManager.ThrowBadRequest<RegisterLotsResultDto>(
                "No se pueden crear tramos en una sección que ya tiene racks registrados.",
                "ERP:SECTION_HAS_RACKS");

        var lotsToCreate = request.ToLotEntities();

        var usedWidth = await _unitOfWork.Lots.Entities
            .Where(l => l.SectionId == request.SectionId)
            .SumAsync(l => l.WidthMetres, cancellationToken);

        var requestedWidth = lotsToCreate.Sum(l => l.WidthMetres);

        if (usedWidth + requestedWidth > section.LengthMetres)
            return _errorManager.ThrowBadRequest<RegisterLotsResultDto>(
                $"La sección no tiene espacio suficiente para los tramos solicitados " +
                $"(ocupado {usedWidth}m + solicitado {requestedWidth}m > disponible {section.LengthMetres}m).",
                "ERP:SECTION_LENGTH_EXCEEDED");

        var duplicatedCodes = lotsToCreate
            .GroupBy(l => l.Code)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedCodes.Count > 0)
            return _errorManager.ThrowBadRequest<RegisterLotsResultDto>(
                $"Se generaron códigos duplicados en la solicitud: {string.Join(", ", duplicatedCodes)}.",
                "ERP:LOT_CODE_DUPLICATED_IN_REQUEST");

        var requestedCodes = lotsToCreate.Select(l => l.Code).ToList();

        var existingLots = await _mapper.ProjectTo<LotSummaryDto>(
                _unitOfWork.Lots.Entities.Where(l =>
                    l.SectionId == request.SectionId && requestedCodes.Contains(l.Code)))
            .ToListAsync(cancellationToken);

        if (existingLots.Count > 0)
            return _errorManager.ThrowBadRequest<RegisterLotsResultDto>(
                $"Ya existen tramos con estos códigos en la sección: {string.Join(", ", existingLots.Select(l => l.Code))}.",
                "ERP:LOT_CODE_ALREADY_EXISTS");

        foreach (var lot in lotsToCreate)
            await _unitOfWork.Lots.RegisterLot(lot);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterLotsResultDto
        {
            SectionId = request.SectionId,
            TotalRequested = lotsToCreate.Count,
            TotalCreated = lotsToCreate.Count,
            Lots = _mapper.Map<List<LotSummaryDto>>(lotsToCreate)
        };
    }
}