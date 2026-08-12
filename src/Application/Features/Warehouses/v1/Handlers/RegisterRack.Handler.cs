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

public class RegisterRacksBulkHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<RegisterRacksBulkCommand, RegisterRacksBulkResultDto>(unitOfWork, errorManager)
{
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

        var estanteCode = string.IsNullOrWhiteSpace(request.ShelfCode)
            ? section.Code.Replace("-", string.Empty)
            : request.ShelfCode;

        var racksToCreate = request.ToRackEntities(estanteCode);

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

        return new RegisterRacksBulkResultDto
        {
            SectionId       = request.SectionId,
            TotalRequested  = racksToCreate.Count,
            TotalCreated    = racksToCreate.Count,
            Racks = racksToCreate.Select(r => new RackSummaryDto
            {
                RackId      = r.Id,
                Code        = r.Code,
                LevelNumber = r.LevelNumber,
                RowNumber   = r.RowNumber
            }).ToList()
        };
    }
}