using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Handlers
{
    public class CreateServiceOrderHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ICodeGenerator _codeGenerator) : BaseValidatorHandler<CreateServiceOrderCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso para realizar esta operación", "ERP:INVALID_ACCESS");
            }

            var operationOrderInProgress = await _unitOfWork.OperationalOrders.Entities
                .Where(po => po.Status != OperationalOrderStatus.Completed)
                .Where(po => po.Id == request.OperationalOrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (operationOrderInProgress is null)
            {
                return _errorManager.ThrowBadRequest<Unit>("La orden operativa seleccionada no existe o ya ha sido cerrada!", "ERP:INVALID_PO");
            }

            var operationalService = await _unitOfWork.OperationalServices.Entities
                .Where(ops => ops.IsActive)
                .Where(ops => ops.Id == request.OperationalServiceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (operationalService is null)
            {
                return _errorManager.ThrowBadRequest<Unit>("El servicio seleccionado no existe en nuestra base de datos", "ERP:SERVICE_INVALID");
            }

            var (IsSuccess, Code) = await _codeGenerator.GenerateUniqueCodeToServiceOrder();

            if (!IsSuccess)
            {
                return _errorManager.ThrowBadRequest<Unit>("Ocurrio un error la generar la solicitud", "ERP:CODE_GENERATOR");
            }

            var serviceOrderEntity = ServicesOrderMapper.ToServiceOrderEntity(request, Code);
            serviceOrderEntity.CreatedByUserId = access.User.Id;

            await _unitOfWork.ServicesOrders.RegisterServicesOrder(serviceOrderEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}