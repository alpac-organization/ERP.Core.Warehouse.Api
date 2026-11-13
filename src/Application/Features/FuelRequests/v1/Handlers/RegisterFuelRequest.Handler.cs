using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Domain.Entities.Exceptions;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Commands;

using ERP.Core.Application.Commons.Interfaces.AWS;
using ERP.Core.Notification.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Handlers;

public class RegisterFuelRequestsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ICodeGenerator codeGenerator,
    IEmailNotificationService emailService, ISimpleNotificationServices pushService, ILogger<RegisterFuelRequestsHandler> logger)
    : BaseValidatorHandler<RegisterFuelRequestsCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(RegisterFuelRequestsCommand request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var areaError = await SupplyRequestSharedHelper.ValidateInventoryAreaAsync(_unitOfWork, _errorManager, access.Profile.AreaId, ct);
        if (areaError is not null) return areaError.Value;

        var machineryError = await ValidateMachineryAsync(request.MachineryId, access.Profile.BranchId, ct);
        if (machineryError is not null) return machineryError.Value;

        var stockError = await ValidateFuelStockAsync(request.ProductStockId, request.RequestedQuantity, access.Profile.BranchId, access.Profile.CostCenterId, ct);
        if (stockError is not null) return stockError.Value;

        var productStock = await _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => ps.Id == request.ProductStockId && ps.DeletedAt == null)
            .Select(ps => new
            {
                ps.UnitMeasureId
            })
            .FirstAsync(ct);

        logger.LogInformation("📝Creando solicitud de combustible.");

        var (branchCode, branchCodeError) = await SupplyRequestSharedHelper.GetBranchCodeAsync(_unitOfWork, _errorManager, access.Profile.BranchId, ct);
        if (branchCodeError is not null) return branchCodeError.Value;

        // Comparación de sucursal por código
        var isCorinto = branchCode!.Contains("ALP-CTO", StringComparison.OrdinalIgnoreCase);

        if (isCorinto)
        {
            if (!request.CustomerId.HasValue || request.CustomerId.Value == Guid.Empty)
                return _errorManager.ThrowBadRequest<Unit>("Debe asignar un cliente.", "ERP:CUSTOMER_REQUIRED");

            var customer = await _unitOfWork.Customers.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId.Value && c.IsActive, ct);

            if (customer is null)
                return _errorManager.ThrowBadRequest<Unit>("El cliente no existe o está inactivo.", "ERP:CUSTOMER_NOT_FOUND");
        }
        else
        {
            // Managua: se mantiene como está (sin cliente)
            request.CustomerId = null;
        }

        var lowStockItems = new List<(string Name, string Code, decimal Available, decimal Minimum)>();

        try
        {
            await _unitOfWork.ExecuteWithUniqueCodeRetryAsync(async token =>
            {
                var (isSuccess, code) = await codeGenerator.GenerateCodeToFuelRequests(branchCode!, token);

                if (!isSuccess || string.IsNullOrWhiteSpace(code))
                    throw new CoreException(new ErrorResponse(500, "ERP:INTERNAL_ERROR", "No se pudo generar el código para la solicitud."));

                var entity = request.ToFuelRequestEntity(access.Profile, code, productStock.UnitMeasureId);

                await _unitOfWork.FuelRequests.RegisterFuelRequest(entity);

                var reservedByStock = new Dictionary<Guid, decimal>
                {
                    [request.ProductStockId] = request.RequestedQuantity
                };
                lowStockItems.Clear();
                lowStockItems.AddRange(await StockReservationHelper.ReserveStockAsync(_unitOfWork, reservedByStock, token));

                await _unitOfWork.SaveChangesAsync(token);

                logger.LogInformation("✅Solicitud {Code} registrada con éxito.", code);

                await SendEmailNotificationAsync(code, lowStockItems, token);
                await SendPushNotificationAsync(code, lowStockItems, token);
            }, ct);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Error de persistencia al registrar la solicitud de combustible.");
            return _errorManager.ThrowBadRequest<Unit>("No se pudo registrar la solicitud de combustible.", "ERP:FUEL_REQUEST_PERSISTENCE_ERROR");
        }

        return Unit.Value;
    }

    private Task SendEmailNotificationAsync(string code, List<(string Name, string Code, decimal Available, decimal Minimum)> lowStockItems, CancellationToken ct)
    {
        if (lowStockItems.Count == 0) return Task.CompletedTask;

        var body = LowStockNotificationHelper.BuildEmailBody(lowStockItems, code, DateTime.Now);
        // TODO: enviar cuando exista la lista de personas a notificar
        logger.LogInformation("Cuerpo de email de stock bajo generado para {Code} ({Length} chars).", code, body.Length);
        return Task.CompletedTask;
    }

    private Task SendPushNotificationAsync(string code, List<(string Name, string Code, decimal Available, decimal Minimum)> lowStockItems, CancellationToken ct)
    {
        if (lowStockItems.Count == 0) return Task.CompletedTask;

        var body = LowStockNotificationHelper.BuildPushBody(lowStockItems, code);
        // TODO: enviar cuando existan destinatarios/endpoints
        logger.LogInformation("Payload push de stock bajo generado para {Code}: {Body}", code, body);
        return Task.CompletedTask;
    }

    private async Task<Unit?> ValidateMachineryAsync(Guid machineryId, Guid branchId, CancellationToken ct)
    {
        var machinery = await _unitOfWork.Machineries.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == machineryId && m.DeletedAt == null, ct);

        if (machinery is null) return _errorManager.ThrowBadRequest<Unit>("La maquinaria no existe.", "ERP:MACHINERY_NOT_FOUND");

        if (!machinery.IsActive) return _errorManager.ThrowBadRequest<Unit>("La maquinaria está inactiva.", "ERP:MACHINERY_INACTIVE");

        if (machinery.BranchId != branchId) return _errorManager.ThrowBadRequest<Unit>("La maquinaria no pertenece a la sucursal del usuario.", "ERP:MACHINERY_BRANCH_MISMATCH");

        return null;
    }

    private async Task<Unit?> ValidateFuelStockAsync(Guid productStockId, decimal requestedQuantity, Guid branchId, Guid costCenterId, CancellationToken ct)
    {
        var stock = await _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => ps.Id == productStockId && ps.DeletedAt == null)
            .Select(ps => new
            {
                ps.Id,
                ps.IsActive,
                ps.AvailableQuantity,
                BranchId = ps.Product.BranchId,
                CostCenterId = ps.CostCenterId,
                Name = ps.Product.ProductName ?? ps.Product.Code,
                Unit = ps.UnitMeasure.Symbol
            })
            .FirstOrDefaultAsync(ct);

        if (stock is null) return _errorManager.ThrowBadRequest<Unit>("El stock de combustible no existe.", "ERP:PRODUCT_STOCK_NOT_FOUND");

        if (stock.BranchId != branchId) return _errorManager.ThrowBadRequest<Unit>("El stock de combustible no pertenece a la sucursal del usuario.", "ERP:PRODUCT_STOCK_BRANCH_MISMATCH");

        if (stock.CostCenterId != costCenterId) return _errorManager.ThrowBadRequest<Unit>("El stock de combustible no pertenece a su centro de costo.", "ERP:PRODUCT_STOCK_COST_CENTER_MISMATCH");

        if (!stock.IsActive) return _errorManager.ThrowBadRequest<Unit>("El stock de combustible está inactivo.", "ERP:PRODUCT_STOCK_INACTIVE");

        if (stock.AvailableQuantity <= 0)
            return _errorManager.ThrowBadRequest<Unit>("El stock de combustible no tiene stock disponible.", "ERP:PRODUCT_STOCK_EMPTY");

        if (requestedQuantity > stock.AvailableQuantity) return _errorManager.ThrowBadRequest<Unit>(
            $"La cantidad solicitada supera el stock disponible: {stock.Name} (solicitado: {requestedQuantity} {stock.Unit}, disponible: {stock.AvailableQuantity} {stock.Unit}).",
            "ERP:INSUFFICIENT_STOCK");

        return null;
    }
}