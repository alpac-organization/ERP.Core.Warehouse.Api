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
    public class ReceptionInformationOperationalHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
        : BaseValidatorHandler<ReceptionInformationOperationalCommand, Unit>(_unitOfWork, _errorManager)
    {
        public override async Task<Unit> Handle(ReceptionInformationOperationalCommand request, CancellationToken cancellationToken)
        {
            #region Validación de acceso

            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            #endregion

            #region Cargar la orden operacional

            var operationalOrder = await _unitOfWork.OperationalOrders.Entities
                .Where(po => po.Id == request.OperationalOrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (operationalOrder is null)
            {
                return _errorManager.ThrowNotFound<Unit>(
                    $"Operational order with ID {request.OperationalOrderId} not found.",
                    "ERP:01");
            }

            #endregion

            #region Validación de cliente

            var customerChanged = request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty;

            if (!operationalOrder.CustomerId.HasValue && !customerChanged)
            {
                return _errorManager.ThrowBadRequest<Unit>(
                    "Debe asignar un cliente a la orden operacional.",
                    "ERP:CUSTOMER_REQUIRED");
            }

            if (customerChanged)
            {
                var customer = await _unitOfWork.Customers.Entities
                    .Where(c => c.IsActive && c.Id == request.CustomerId!.Value)
                    .FirstOrDefaultAsync(cancellationToken);

                if (customer is null)
                {
                    return _errorManager.ThrowNotFound<Unit>(
                        "El cliente seleccionado no existe en nuestros registros.",
                        "ERP:04");
                }

                operationalOrder.CustomerId = request.CustomerId!.Value;
            }

            #endregion

            #region Campos escalares de la orden

            operationalOrder.Weight = request.MerchandiseWeight ?? operationalOrder.Weight;
            operationalOrder.PackagesCount = request.PackageAmount ?? operationalOrder.PackagesCount;
            operationalOrder.ShippingCompany = request.ShippingCompany ?? operationalOrder.ShippingCompany;
            operationalOrder.Consignee = request.Consignee ?? operationalOrder.Consignee;
            operationalOrder.Sender = request.Sender ?? operationalOrder.Sender;
            operationalOrder.IsAlerted = request.IsAlerted ?? operationalOrder.IsAlerted;

            #endregion

            #region Crear asignaciones operacionales

            // Se pueden agregar mercancías en cualquier PATCH.
            // Cada llamada con items agrega nuevas AssignmentOperational.

            var validMerchandises = (request.Merchandises ?? [])
                .Where(m => !string.IsNullOrWhiteSpace(m.Merchandise)
                            || !string.IsNullOrWhiteSpace(m.MerchandiseDescription))
                .ToList();

            if (validMerchandises.Count > 0)
            {
                var createResult = await CreateMerchandisesAsync(
                    operationalOrder,
                    validMerchandises,
                    access.User.Id);

                if (!createResult.IsSuccess)
                {
                    return (Unit)createResult.ErrorResponse!;
                }

                operationalOrder.HasAssignmentOperationalActive = true;
                operationalOrder.Status = OperationalOrderStatus.Assignment;
            }

            #endregion

            #region Persistir

            await _unitOfWork.OperationalOrders.UpdateAsync(operationalOrder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;

            #endregion
        }

        #region Crear mercancías

        private async Task<Result> CreateMerchandisesAsync(
            OperationalOrder operationalOrder,
            List<MerchandiseCreate> incoming,
            Guid userId)
        {
            // --- Duplicados de nombre entre items ---

            var duplicateNames = incoming
                .Where(m => !string.IsNullOrWhiteSpace(m.Merchandise))
                .GroupBy(m => m.Merchandise!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                return Result.Failure(_errorManager.ThrowBadRequest<Unit>(
                    "La lista contiene mercancías duplicadas: " + string.Join(", ", duplicateNames),
                    "ERP:DUPLICATE_MERCHANDISE"));
            }

            // --- Create ---

            foreach (var item in incoming)
            {
                var newAssignment = new AssignmentOperational
                {
                    Id = Guid.NewGuid(),
                    IsActive = true,
                    OperationalOrderId = operationalOrder.Id,
                    Merchandise = item.Merchandise?.Trim(),
                    MerchandiseDescription = item.MerchandiseDescription?.Trim(),
                    HasMerchandiseDescription = !string.IsNullOrWhiteSpace(item.MerchandiseDescription),
                    Status = AssignmentOperationalStatus.None,
                    Category = MerchandiseCategory.None,
                    DestinationType = DestinationType.None,
                    AdditionalData = "{}"
                };

                await _unitOfWork.AssignmentOperationals.RegisterAssignmentOperational(newAssignment);
            }

            return Result.Success();
        }

        #endregion

        #region Result interno

        private class Result
        {
            public bool IsSuccess { get; private set; }
            public Unit? ErrorResponse { get; private set; }

            private Result(bool isSuccess, Unit? errorResponse = null)
            {
                IsSuccess = isSuccess;
                ErrorResponse = errorResponse;
            }

            public static Result Success() => new(true);
            public static Result Failure(Unit errorResponse) => new(false, errorResponse);
        }

        #endregion
    }
}