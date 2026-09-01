// Handlers/RegisterRackHandler.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterRacksBulkHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<RacksBulkCommand, bool>(unitOfWork, errorManager)
{

    public override async Task<bool> Handle(RacksBulkCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var sectionInfo = await _unitOfWork.Sections.Entities
            .Where(s => s.Id == request.SectionId && s.IsActive)
            .Select(s => new { s.SectionType })
            .FirstOrDefaultAsync(cancellationToken);

        if (sectionInfo is null)
        {
            return _errorManager.ThrowBadRequest<bool>("La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");
        }
        if (sectionInfo.SectionType == SectionType.Aisle)
            return _errorManager.ThrowBadRequest<bool>("No se pueden crear racks en una sección de tipo pasillo.", "ERP:SECTION_TYPE_NOT_ALLOWED_FOR_RACKS");

        var racksToCreate = request.ToRackEntities();
        var requestedCodes = racksToCreate.Select(r => r.Code).ToList();

        var duplicatedCodes = requestedCodes.GroupBy(c => c)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key)
        .ToList();

        if (duplicatedCodes.Count > 0)
        {
            return _errorManager.ThrowBadRequest<bool>($"Se enviaron códigos duplicados en la solicitud: {string.Join(", ", duplicatedCodes)}.", "ERP:RACK_CODE_DUPLICATED_IN_REQUEST");
        }

        var existingCodes = await _unitOfWork.Racks.Entities
        .Where(r => r.SectionId == request.SectionId && requestedCodes.Contains(r.Code))
        .Select(r => r.Code)
        .ToListAsync(cancellationToken);

        if (existingCodes.Count > 0)
            return _errorManager.ThrowBadRequest<bool>($"Ya existen racks con estos códigos en la sección: {string.Join(", ", existingCodes)}.", "ERP:RACK_CODE_ALREADY_EXISTS");

        foreach (var rack in racksToCreate)
            await _unitOfWork.Racks.RegisterRack(rack);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
