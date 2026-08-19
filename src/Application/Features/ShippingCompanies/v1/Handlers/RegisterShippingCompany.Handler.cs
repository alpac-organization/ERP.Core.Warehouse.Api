using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Commands;
using ShippingCompanyEntity = ERP.Core.Database.Domain.Entities.Catalogs.ShippingCompanies;

namespace ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Handlers;

public class RegisterShippingCompanyHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<RegisterShippingCompanyCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(RegisterShippingCompanyCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        // Sanitizar el nombre
        var sanitizedName = request.Name.SanitizeAlphanumeric();
        
        if (string.IsNullOrEmpty(sanitizedName))
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El nombre de la naviera es obligatorio.",
                "ERP:INVALID_SHIPPING_COMPANY_NAME");
        }

        // Validar que no exista una naviera con el mismo nombre
        var existingShippingCompany = await _unitOfWork.ShippingComapanies.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.Name.ToLower() == sanitizedName.ToLower() && sc.DeletedAt == null, cancellationToken);

        if (existingShippingCompany != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Ya existe una naviera con este nombre.",
                "ERP:SHIPPING_COMPANY_ALREADY_EXISTS");
        }

        // Crear la nueva naviera
        var shippingCompany = mapper.Map<ShippingCompanyEntity>(request);

        await _unitOfWork.ShippingComapanies.RegisterShippingCompany(shippingCompany);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}