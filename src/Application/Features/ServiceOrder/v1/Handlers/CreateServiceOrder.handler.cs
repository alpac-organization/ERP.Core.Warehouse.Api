using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;
using ServiceOrderEntity = ERP.Core.Database.Domain.Entities.Warehouse.ServiceOrder;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using AutoMapper;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Handlers
{
    public class CreateServiceOrderHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<CreateServiceOrderCommand, CreateServiceOrderResponse>(unitOfWork, errorManager)
    {
        public override async Task<CreateServiceOrderResponse> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
        {
            // 1. Validación de acceso
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            // 2. Consulta de Sucursal (Necesaria para el Code)
            var branch = await _unitOfWork.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null)
                return _errorManager.ThrowBadRequest<CreateServiceOrderResponse>(
                    $"Sucursal {request.BranchId} no encontrada.",
                    "ERP:BRANCH_NOT_FOUND");

            // 3. Cliente
            var customerExists = await _unitOfWork.Customers.Entities
                .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
            if (!customerExists)
                return _errorManager.ThrowBadRequest<CreateServiceOrderResponse>(
                    "El cliente indicado no existe.",
                    "ERP:CUSTOMER_NOT_FOUND");

            // 3.b Validar que no exista una OS abierta para el mismo cliente en la misma sucursal
            var hasOpenOrder = await _unitOfWork.ServiceOrders.Entities
                .AnyAsync(so =>
                    so.CustomerId == request.CustomerId &&
                    (so.Status == OSStatus.Pending || so.Status == OSStatus.InProgress),
                    cancellationToken);

            if (hasOpenOrder)
            {
                return _errorManager.ThrowBadRequest<CreateServiceOrderResponse>(
                    "El cliente ya tiene una orden de servicio abierta.",
                    "ERP:CUSTOMER_HAS_OPEN_SERVICE_ORDER");
            }

            // 4. Generación del Code (Autogenerado según requerimiento)
            var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var codePrefix = $"OS-{branch.BranchCode}-{todayStr}-";

            var existingOrdersCount = await _unitOfWork.ServiceOrders.Entities
                .CountAsync(so => so.Code.StartsWith(codePrefix), cancellationToken);

            string serviceOrderCode = $"{codePrefix}{existingOrdersCount:D2}";

            // 5. Mapeo
            var serviceOrder = mapper.Map<ServiceOrderEntity>(request);
            serviceOrder.Id = Guid.NewGuid();
            serviceOrder.Code = serviceOrderCode;
            serviceOrder.Status = OSStatus.InProgress;


            // 6. Persistencia
            await _unitOfWork.ServiceOrders.RegisterServiceOrder(serviceOrder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<CreateServiceOrderResponse>(serviceOrder);
        }
    }
}