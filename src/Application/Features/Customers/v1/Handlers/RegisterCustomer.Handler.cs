using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Domain.Entities.Exceptions;

using CustomerEntity = ERP.Core.Database.Domain.Entities.Operations.Customers;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;
using ERP.Core.Database.Application.Commons.Interfaces.Services;

namespace ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Handlers;

public class RegisterCustomerHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ILogger<RegisterCustomerHandler> logger, ICodeGenerator codeGenerator)
    : BaseValidatorHandler<RegisterCustomerCommand, RegisterCustomerResult>(unitOfWork, errorManager)
{
    public override async Task<RegisterCustomerResult> Handle(RegisterCustomerCommand request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Manager))
            return _errorManager.ThrowUnauthorized<RegisterCustomerResult>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");

        logger.LogInformation("Iniciando registro de cliente. CompanyId: {CompanyId}", access.Profile.CompanyId);

        var companyId = access.Profile.CompanyId;

        // El perfil no llega con la navegación Branch cargada: se resuelve por BranchId.
        var branch = await _unitOfWork.Branches.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == access.Profile.BranchId, ct);

        if (branch is null || string.IsNullOrWhiteSpace(branch.BranchCode))
            return _errorManager.ThrowBadRequest<RegisterCustomerResult>("No se pudo determinar la sucursal del usuario.", "ERP:BRANCH_NOT_FOUND");

        var identificationNumber = request.IdentificationNumber!.Trim();

        var duplicated = await _unitOfWork.Customers.Entities
            .AsNoTracking()
            .AnyAsync(c => c.CompanyId == companyId && c.IdentificationNumber == identificationNumber, ct);

        if (duplicated)
            return _errorManager.ThrowBadRequest<RegisterCustomerResult>("El número de identificación ya está registrado.", "ERP:CUSTOMER_DUPLICATED");

        CustomerEntity? customer = null;

        try
        {
            await _unitOfWork.ExecuteWithUniqueCodeRetryAsync(async token =>
            {
                // El código se genera dentro de la transacción para que el lock por prefijo del Core sea efectivo.
                var (isSuccess, code) = await codeGenerator.GenerateCodeToCustomers(branch.BranchCode!, token);

                if (!isSuccess || string.IsNullOrWhiteSpace(code))
                    throw new CoreException(new ErrorResponse(500, "ERP:INTERNAL_ERROR", "Ocurrió un error al generar el código de cliente"));

                customer = request.ToCustomerEntity(companyId, code);

                await _unitOfWork.Customers.RegisterCustomer(customer);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error de persistencia al registrar el cliente");
            return _errorManager.ThrowBadRequest<RegisterCustomerResult>("El número de identificación ya está registrado.", "ERP:CUSTOMER_DUPLICATED");
        }

        logger.LogInformation("Cliente registrado correctamente. CustomerId: {CustomerId}", customer!.Id);

        return new RegisterCustomerResult
        {
            CustomerId = customer!.Id,
            CIFCode = customer.CIFCode
        };
    }
}
