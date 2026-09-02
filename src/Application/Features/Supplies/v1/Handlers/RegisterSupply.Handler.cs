using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Commands;
using SuppliesEntity = ERP.Core.Database.Domain.Entities.Catalogs.Supplies;

namespace ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Handlers;

public class RegisterSupplyHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<RegisterSupplyCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(RegisterSupplyCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var sanitizedName = request.Name.SanitizeAlphanumeric();

        if (string.IsNullOrEmpty(sanitizedName))
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El nombre del insumo es obligatorio.",
                "ERP:INVALID_SUPPLY_NAME");
        }

        var existingSupply = await _unitOfWork.Supplies.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(s => string.Equals(s.Name, sanitizedName, StringComparison.OrdinalIgnoreCase) && s.DeletedAt == null, cancellationToken);

        if (existingSupply != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Ya existe un insumo con este nombre.",
                "ERP:SUPPLY_ALREADY_EXISTS");
        }

        var supply = mapper.Map<SuppliesEntity>(request);

        await _unitOfWork.Supplies.InsertSupply(supply);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
