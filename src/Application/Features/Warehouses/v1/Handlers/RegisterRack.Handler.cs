// Handlers/RegisterRackHandler.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterRacksBulkHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<RegisterRacksBulkCommand, RegisterRacksBulkResultDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<RegisterRacksBulkResultDto> Handle(RegisterRacksBulkCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.IsActive, cancellationToken);

        if (section is null)
            return _errorManager.ThrowBadRequest<RegisterRacksBulkResultDto>(
                "La sección indicada no existe o no está activa.",
                "ERP:SECTION_NOT_FOUND");

        var shelfCode = string.IsNullOrWhiteSpace(request.ShelfCode)
            ? section.Code.Replace("-", string.Empty)
            : request.ShelfCode;

        var shelfPrefix = $"{shelfCode}-D";

        var shelfCodeUsedInOtherSection = await _unitOfWork.Racks.Entities
            .Where(r => r.SectionId != request.SectionId && r.Code.StartsWith(shelfPrefix))
            .Join(_unitOfWork.Sections.Entities,
                r => r.SectionId,
                s => s.Id,
                (r, s) => s)
            .AnyAsync(s => s.WarehouseId == section.WarehouseId, cancellationToken);

        if (shelfCodeUsedInOtherSection)
            return _errorManager.ThrowBadRequest<RegisterRacksBulkResultDto>(
                $"El código de estante '{shelfCode}' ya está en uso en otra sección de este almacén.",
                "ERP:SHELF_CODE_ALREADY_USED_IN_WAREHOUSE");

        // Trae RowNumber, LevelNumber y LengthMetres en una sola pasada
        var existingRacks = await _unitOfWork.Racks.Entities
            .Where(r => r.SectionId == request.SectionId)
            .Select(r => new { r.RowNumber, r.LevelNumber, r.LengthMetres })
            .ToListAsync(cancellationToken);

        var lastRowByLevel = existingRacks
            .GroupBy(r => r.LevelNumber)
            .ToDictionary(g => g.Key, g => g.Max(r => r.RowNumber));

        var usedLengthByLevel = existingRacks
            .GroupBy(r => r.LevelNumber)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.LengthMetres));

        var nextDepositNumber = request.StartingDepositNumber ?? (existingRacks.Count + 1);

        var racksToCreate = request.ToRackEntities(shelfCode, nextDepositNumber, lastRowByLevel);

        // Capacidad por nivel: la profundidad acumulada de racks no puede superar el largo de la sección
        var overCapacityLevels = racksToCreate
            .GroupBy(r => r.LevelNumber)
            .Select(g => new
            {
                LevelNumber = g.Key,
                UsedBefore = usedLengthByLevel.GetValueOrDefault(g.Key, 0m),
                Requested = g.Sum(r => r.LengthMetres)
            })
            .Where(x => x.UsedBefore + x.Requested > section.LengthMetres)
            .ToList();

        if (overCapacityLevels.Count > 0)
        {
            var detail = string.Join("; ", overCapacityLevels.Select(x =>
                $"nivel {x.LevelNumber}: ocupado {x.UsedBefore}m + solicitado {x.Requested}m > disponible {section.LengthMetres}m"));

            return _errorManager.ThrowBadRequest<RegisterRacksBulkResultDto>(
                $"La sección no tiene capacidad suficiente para los racks solicitados ({detail}).",
                "ERP:SECTION_LENGTH_EXCEEDED");
        }

        // Duplicados generados dentro de la misma solicitud
        var duplicatedCodes = racksToCreate
            .GroupBy(r => r.Code)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedCodes.Count > 0)
            return _errorManager.ThrowBadRequest<RegisterRacksBulkResultDto>(
                $"Se generaron códigos duplicados en la solicitud: {string.Join(", ", duplicatedCodes)}.",
                "ERP:RACK_CODE_DUPLICATED_IN_REQUEST");

        var duplicatedPositions = racksToCreate
            .GroupBy(r => (r.RowNumber, r.LevelNumber))
            .Any(g => g.Count() > 1);

        if (duplicatedPositions)
            return _errorManager.ThrowBadRequest<RegisterRacksBulkResultDto>(
                "Se generaron posiciones (fila/nivel) duplicadas en la solicitud.",
                "ERP:RACK_POSITION_DUPLICATED_IN_REQUEST");

        // Choques contra lo que ya existe en la sección
        var codes = racksToCreate.Select(r => r.Code).ToList();
        var existingCodes = await _unitOfWork.Racks.Entities
            .Where(r => r.SectionId == request.SectionId && codes.Contains(r.Code))
            .Select(r => r.Code)
            .ToListAsync(cancellationToken);

        if (existingCodes.Count > 0)
            return _errorManager.ThrowBadRequest<RegisterRacksBulkResultDto>(
                $"Ya existen racks con estos códigos en la sección: {string.Join(", ", existingCodes)}.",
                "ERP:RACK_CODE_ALREADY_EXISTS");

        var existingPositions = await _unitOfWork.Racks.Entities
            .Where(r => r.SectionId == request.SectionId)
            .Select(r => new { r.RowNumber, r.LevelNumber })
            .ToListAsync(cancellationToken);

        var positionCollision = racksToCreate.Any(r =>
            existingPositions.Any(p => p.RowNumber == r.RowNumber && p.LevelNumber == r.LevelNumber));

        if (positionCollision)
            return _errorManager.ThrowBadRequest<RegisterRacksBulkResultDto>(
                "Alguna de las posiciones (fila/nivel) ya está ocupada en esta sección.",
                "ERP:RACK_POSITION_ALREADY_TAKEN");

        foreach (var rack in racksToCreate)
            await _unitOfWork.Racks.RegisterRack(rack);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var allLevelNumbers = existingRacks.Select(r => r.LevelNumber)
            .Union(racksToCreate.Select(r => r.LevelNumber))
            .Distinct();

        var levelsCapacity = allLevelNumbers
            .Select(levelNumber =>
            {
                var usedBefore = usedLengthByLevel.GetValueOrDefault(levelNumber, 0m);
                var addedNow = racksToCreate.Where(r => r.LevelNumber == levelNumber).Sum(r => r.LengthMetres);
                var usedTotal = usedBefore + addedNow;

                var racksCountBefore = existingRacks.Count(r => r.LevelNumber == levelNumber);
                var racksCountNow = racksCountBefore + racksToCreate.Count(r => r.LevelNumber == levelNumber);

                return new LevelCapacityDto
                {
                    LevelNumber = levelNumber,
                    RacksCount = racksCountNow,
                    UsedLengthMetres = usedTotal,
                    AvailableLengthMetres = section.LengthMetres - usedTotal
                };
            })
            .OrderBy(x => x.LevelNumber)
            .ToList();

        return new RegisterRacksBulkResultDto
        {
            SectionId = request.SectionId,
            SectionLengthMetres = section.LengthMetres,
            TotalRequested = racksToCreate.Count,
            TotalCreated = racksToCreate.Count,
            // Racks = _mapper.Map<List<RackSummaryDto>>(racksToCreate),
            LevelCapacity = levelsCapacity
        };
    }
}