using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Manager.Api.Application.Commons.Bases;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

// Alias explícito hacia la entidad en el Core
using ServiceOrderEntity = ERP.Core.Database.Domain.Entities.Warehouse.ServiceOrder;

namespace ERP.Core.Warehouse.Api.Application.Features.CreateServiceOrder.v1.Handlers
{
    public class CreateServiceOrderHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
        : AlpacBaseHandler<CreateServiceOrderCommand, CreateServiceOrderResponse>(unitOfWork, errorManager)
    {
        public override async Task<CreateServiceOrderResponse> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
        {
            // 1. Validación de acceso
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            // 2. Consulta de Sucursal (Necesaria para el Code)
            var branch = await _unitOfWork.Branches
                .FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
            if (branch == null) throw new KeyNotFoundException($"Sucursal {request.BranchId} no encontrada.");

            // 3. Generación del Code (Autogenerado según requerimiento)
            var todayStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var codePrefix = $"OS-{branch.BranchCode}-{todayStr}-";

            var existingOrdersCount = await EntityFrameworkQueryableExtensions.CountAsync(
                _unitOfWork.ServiceOrders.Entities,
                so => so.Code.StartsWith(codePrefix),
                cancellationToken);

            string serviceOrderCode = $"{codePrefix}{existingOrdersCount:D2}";

            // 4. Instanciación con los campos requeridos
            var serviceOrder = new ServiceOrderEntity
            {
                Id = Guid.NewGuid(),
                Code = serviceOrderCode,          // Autogenerado
                BranchId = request.BranchId,      // Recibido
                Status = OSStatus.Pendding,       // Predeterminado
                IsCreatedFromPortal = false,      // Fijo en false según requerimiento
                Observations = request.Observations,
            };

            // 5. Persistencia
            await _unitOfWork.ServiceOrders.RegisterServiceOrder(serviceOrder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateServiceOrderResponse(
                serviceOrder.Id, 
                serviceOrder.Code, 
                serviceOrder.Status.ToString()
            );
        }
    }
}