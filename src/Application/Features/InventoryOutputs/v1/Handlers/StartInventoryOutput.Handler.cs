using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Commands;

using OutputEntity = ERP.Core.Database.Domain.Entities.Inventory.InventoryOutputs;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryOutputs.v1.Handlers
{
    public class StartInventoryOutputHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
        : BaseValidatorHandler<StartInventoryOutputCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(StartInventoryOutputCommand request, CancellationToken ct)
        {
            var (access, output, error) = await ValidateAccessAndOutputAsync(request, ct);
            if (error is not null) return error ?? Unit.Value;

            var requestedByStock = BuildRequestedQuantities(output!);
            var (toDispatch, validationError) = BuildDispatchQuantities(request, requestedByStock);
            if (validationError is not null) return validationError ?? Unit.Value;

            await RegisterDispatchItemsAsync(output!.Id, toDispatch!, ct);

            output.Status = InventoryOutputsStatus.InProgress;
            output.DispatchedJustification = request.DispatchedJustification
                ?? (request.IsAllDispached ? "Despacho completo" : "Despacho parcial");
            output.IsAllDispached = request.IsAllDispached;

            await _unitOfWork.InventoryOutputs.UpdateAsync(output);
            await _unitOfWork.SaveChangesAsync(ct);
            return Unit.Value;
        }

        private async Task<(AccessValidationResult<Unit> access, OutputEntity? output, Unit? error)> ValidateAccessAndOutputAsync(
            StartInventoryOutputCommand request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
            if (!access.IsSuccess)
                return (access, null, access.ErrorResponse);

            if (access.Role?.RoleType is not (RoleType.Administrator or RoleType.Supervisor))
                return (access, null, _errorManager.ThrowUnauthorized<Unit>("No tiene acceso para realizar esta acción", "ERP:INVALID_ACCESS"));

            var output = await _unitOfWork.InventoryOutputs.Entities
                .Include(o => o.SupplyRequest)
                    .ThenInclude(s => s.SupplyRequestItems)
                .Include(o => o.FuelRequest)
                .Include(o => o.InventoryOutputsItems)
                .FirstOrDefaultAsync(o => o.Id == request.InventoryOutputId && o.DeletedAt == null, ct);

            if (output is null)
                return (access, null, _errorManager.ThrowNotFound<Unit>("La salida de inventario no fue encontrada.", "ERP:INVENTORY_OUTPUT_NOT_FOUND"));

            if (output.Status == InventoryOutputsStatus.InProgress)
                return (access, null, _errorManager.ThrowBadRequest<Unit>("La salida ya está en proceso de despacho.", "ERP:INVENTORY_OUTPUT_IN_PROGRESS"));

            if (output.Status != InventoryOutputsStatus.OutputPending)
                return (access, null, _errorManager.ThrowBadRequest<Unit>("La salida no está pendiente para iniciar despacho.", "ERP:INVENTORY_OUTPUT_NOT_PENDING"));

            return (access, output, null);
        }

        private static Dictionary<Guid, decimal> BuildRequestedQuantities(OutputEntity output)
        {
            var requestedByStock = new Dictionary<Guid, decimal>();

            if (output.SupplyRequest is not null)
            {
                foreach (var item in output.SupplyRequest.SupplyRequestItems)
                {
                    if (!item.IsActive) continue;
                    if (requestedByStock.TryGetValue(item.ProductStockId, out var current))
                        requestedByStock[item.ProductStockId] = current + item.Amount;
                    else
                        requestedByStock[item.ProductStockId] = item.Amount;
                }
            }

            if (output.FuelRequest is not null)
            {
                var stockId = output.FuelRequest.ProductStockId;
                var qty = output.FuelRequest.RequestedQuantity;
                if (requestedByStock.TryGetValue(stockId, out var current))
                    requestedByStock[stockId] = current + qty;
                else
                    requestedByStock[stockId] = qty;
            }

            return requestedByStock;
        }

        private (Dictionary<Guid, decimal>? toDispatch, Unit? error) BuildDispatchQuantities(
            StartInventoryOutputCommand request,
            Dictionary<Guid, decimal> requestedByStock)
        {
            var toDispatch = new Dictionary<Guid, decimal>();

            if (request.IsAllDispached)
            {
                foreach (var kv in requestedByStock)
                    toDispatch[kv.Key] = kv.Value;
                return (toDispatch, null);
            }

            foreach (var item in request.Items)
            {
                if (!requestedByStock.ContainsKey(item.ProductStockId))
                    return (null, _errorManager.ThrowBadRequest<Unit>("El ítem no pertenece a la salida.", "ERP:INVALID_OUTPUT_ITEM"));

                if (item.DispatchedQuantity > requestedByStock[item.ProductStockId])
                    return (null, _errorManager.ThrowBadRequest<Unit>("La cantidad a despachar supera la solicitada.", "ERP:INVALID_DISPATCH_QUANTITY"));

                toDispatch[item.ProductStockId] = item.DispatchedQuantity;
            }

            return (toDispatch, null);
        }

        private async Task RegisterDispatchItemsAsync(Guid outputId, Dictionary<Guid, decimal> toDispatch, CancellationToken ct)
        {
            var existingItems = await _unitOfWork.InventoryOutputsItems.Entities
                .Where(i => i.InventoryOutputId == outputId)
                .ToListAsync(ct);

            if (existingItems.Count > 0)
                await _unitOfWork.InventoryOutputsItems.RemoveRangeAsync(existingItems);

            foreach (var (stockId, qty) in toDispatch)
            {
                if (qty <= 0) continue;

                var outputItem = InventoryOutputMapper.ToInventoryOutputItemEntity(outputId, stockId, qty);
                await _unitOfWork.InventoryOutputsItems.RegisterInventoryOutputItem(outputItem);
            }
        }
    }
}