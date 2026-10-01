using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Handlers
{
    public class ReceptionInformationOperationalHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<ReceptionInformationOperationalCommand, Unit>(_unitOfWork, _errorManager)
    {
        public override async Task<Unit> Handle(ReceptionInformationOperationalCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var operationalOrder = await _unitOfWork.OperationalOrders.Entities
                .Where(po => po.Id == request.OperationalOrderId)
                .Include(po => po.Customer)
                .Include(po => po.CostCenter)
                .Include(po => po.HasAssignmentOperationalActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (operationalOrder == null)
            {
                return _errorManager.ThrowNotFound<Unit>($"Operational order with ID {request.OperationalOrderId} not found.", "ERP:01");
            }

            var customerSelected = await _unitOfWork.Customers.Entities
                .Where(c => c.IsActive)
                .Where(c => c.Id == request.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (customerSelected is null)
            {
                return _errorManager.ThrowNotFound<Unit>("El cliente seleccionado no existe en nuestros registros.", "ERP:04");
            }

            operationalOrder.CustomerId = request.CustomerId;
            operationalOrder.Weight = request.MerchandiseWeight;
            operationalOrder.PackagesCount = request.PackageAmount;

            await _unitOfWork.OperationalOrders.UpdateAsync(operationalOrder);

            if (request.HasMerchandise && operationalOrder.HasAssignmentOperationalActive)
            {
                return _errorManager.ThrowBadRequest<Unit>("La información de la mercaderia ya ha sido recepciponada.", "ERP:02");
            }

            if (request.HasMerchandise)
            {
                await _unitOfWork.AssignmentOperationals.RegisterAssignmentOperational(new AssignmentOperational()
                {
                    Status = AssignmentOperationalStatus.None,
                    OperationalOrderId = operationalOrder.Id,
                    Merchandise = request.MerchandiseInformation?.Merchandise,
                    MerchandiseDescription = request.MerchandiseInformation?.MerchandiseDescription,
                    HasMerchandiseDescription = true,  
                });
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}