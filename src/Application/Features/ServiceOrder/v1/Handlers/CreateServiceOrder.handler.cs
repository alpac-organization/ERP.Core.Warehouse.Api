using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Handlers
{
    public class CreateServiceOrderHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<CreateServiceOrderCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
        {
            // 1. Validación de acceso
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            // 2. Consulta de Sucursal (Necesaria para el Code)
            var branch = await _unitOfWork.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return _errorManager.ThrowBadRequest<Unit>(
                    $"Sucursal {request.BranchId} no encontrada.",
                    "ERP:BRANCH_NOT_FOUND");

            // 3. Cliente
            var customerExists = await _unitOfWork.Customers.Entities
                .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
            if (!customerExists)
                return _errorManager.ThrowBadRequest<Unit>(
                    "El cliente indicado no existe.",
                    "ERP:CUSTOMER_NOT_FOUND");

            // 4. Generación del Code (Autogenerado según requerimiento)
            var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var codePrefix = $"OS-{branch.BranchCode}-{todayStr}-";


            // 6. Persistencia


            return Unit.Value;
        }
    }
}