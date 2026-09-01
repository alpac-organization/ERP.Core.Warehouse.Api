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

public class RegisterLotsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<RegisterLotsCommand, bool>(unitOfWork, errorManager)
{

    public override async Task<bool> Handle(RegisterLotsCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        // var section = await _unitOfWork.Sections.Entities
        //     .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.IsActive, cancellationToken);

        var sectionInfo = await _unitOfWork.Sections.Entities
            .Where(s => s.Id == request.SectionId && s.IsActive)
            .Select(s => new
            {
                s.SectionType,
                HasRacks = s.Racks.Any()
            })
            .FirstOrDefaultAsync(cancellationToken);



        if (sectionInfo is null)
            return _errorManager.ThrowBadRequest<bool>(
                "La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

        if (sectionInfo.SectionType == SectionType.Aisle)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden crear tramos en una sección de tipo pasillo.",
                "ERP:SECTION_TYPE_NOT_ALLOWED_FOR_LOTS");

        var sectionHasRacks = await _unitOfWork.Racks.Entities
            .AnyAsync(r => r.SectionId == request.SectionId, cancellationToken);

        if (sectionHasRacks)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pueden crear tramos en una sección que ya tiene racks registrados.",
                "ERP:SECTION_HAS_RACKS");

        var lotsToCreate = request.ToLotEntities();
        var requestedCodes = lotsToCreate.Select(l => l.Code).ToList();

        var duplicatedCodes = requestedCodes.GroupBy(c => c)
           .Where(g => g.Count() > 1)
           .Select(g => g.Key)
           .ToList();


        if (duplicatedCodes.Count > 0)
            return _errorManager.ThrowBadRequest<bool>($"Se enviaron códigos duplicados en la solicitud: {string.Join(", ", duplicatedCodes)}.", "ERP:LOT_CODE_DUPLICATED_IN_REQUEST");

        var existingCodeLots = await _unitOfWork.Lots.Entities
            .Where(l => l.SectionId == request.SectionId && requestedCodes.Contains(l.Code))
            .Select(l => l.Code)
            .ToListAsync(cancellationToken);

        if (existingCodeLots.Count > 0)
            return _errorManager.ThrowBadRequest<bool>(
                $"Ya existen tramos con estos códigos en la sección: {string.Join(", ", existingCodeLots)}.",
                "ERP:LOT_CODE_ALREADY_EXISTS");

        foreach (var lot in lotsToCreate)
        {
            await _unitOfWork.Lots.RegisterLot(lot);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}