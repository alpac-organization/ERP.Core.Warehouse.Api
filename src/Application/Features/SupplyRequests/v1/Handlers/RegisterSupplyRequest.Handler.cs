using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Domain.Entities.Exceptions;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Notification.Application.Commons.Interfaces;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Handlers;

public class RegisterSupplyRequestsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ICodeGenerator codeGenerator,
    IEmailNotificationService emailService, ISimpleNotificationServices pushService, ILogger<RegisterSupplyRequestsHandler> logger)
    : BaseValidatorHandler<RegisterSupplyRequestsCommad, string>(unitOfWork, errorManager)
{
    public override async Task<string> Handle(RegisterSupplyRequestsCommad request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var areaError = await SupplyRequestSharedHelper.ValidateInventoryAreaAsync(_unitOfWork, _errorManager, access.Profile.AreaId, ct);
        if (areaError is not null) return areaError;

        // Validar que la PO exista
        var orderExists = await _unitOfWork.OperationalOrders.Entities
            .AsNoTracking()
            .AnyAsync(op => op.Id == request.OperationalOrderId && op.DeletedAt == null, ct);


        if (!orderExists) return _errorManager.ThrowBadRequest<string>("La Orden Operativa no existe.", "ERP:OPERATIVE_ORDER_NOT_FOUND");


        logger.LogInformation("📝Creando solicitud de inventario.");

        var stockError = await ValidateRequestedStockAsync(request, access.Profile.BranchId, access.Profile.CostCenterId, ct);

        if (stockError is not null) return stockError;

        var assignment = await _unitOfWork.AssignmentOperationals.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.OperationalOrderId == request.OperationalOrderId
                    && a.IsActive && a.DeletedAt == null,
                ct
            );

        if (assignment is null || assignment.WarehouseId is null)
        {
            return _errorManager.ThrowBadRequest<string>(
                "La asignación no tiene un almacén válido para esta orden operativa.",
                "ERP:ASSIGNMENT_WAREHOUSE_NOT_FOUND"
            );
        }

        var (branchCode, branchCodeError) = await SupplyRequestSharedHelper.GetBranchCodeAsync(_unitOfWork, _errorManager, access.Profile.BranchId, ct);
        if (branchCodeError is not null) return branchCodeError;

        string? generatedCode = null;
        var lowStockItems = new List<(string Name, string Code, decimal Available, decimal Minimum)>();

        try
        {
            await _unitOfWork.ExecuteWithUniqueCodeRetryAsync(async token =>
            {
                lowStockItems.Clear();

                var (isSuccess, code) = await codeGenerator.GenerateCodeToSupplyRequests(branchCode!, request.ReasonUse, token);

                if (!isSuccess || string.IsNullOrWhiteSpace(code))
                    throw new CoreException(new ErrorResponse(500, "ERP:INTERNAL_ERROR", "No se pudo generar el código para la solicitud."));

                generatedCode = code;

                var entity = request.ToSupplyRequestEntity(access.Profile, code, assignment.WarehouseId.Value);

                await _unitOfWork.SupplyRequests.RegisterSupplyRequest(entity);

                // Reserva de stock: se resta del disponible por cada stock afectado
                var reservedByStock = request.Items
                    .GroupBy(i => i.ProductStockId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

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
            logger.LogError(ex, "Error de persistencia al registrar la solicitud de insumos.");
            return _errorManager.ThrowBadRequest<string>("No se pudo registrar la solicitud de insumos.", "ERP:SUPPLY_REQUEST_PERSISTENCE_ERROR");
        }

        return generatedCode ?? string.Empty;
    }

    private async Task<string?> ValidateRequestedStockAsync(RegisterSupplyRequestsCommad request, Guid branchId, Guid costCenterId, CancellationToken ct)
    {
        var requested = request.Items
            .GroupBy(r => r.ProductStockId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

        var stockIds = requested.Keys.ToList();

        var stocks = await _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .Where(ps => stockIds.Contains(ps.Id) && ps.DeletedAt == null)
            .Select(ps => new
            {
                ps.Id,
                ps.IsActive,
                ps.AvailableQuantity,
                ps.Product.BranchId,
                ps.CostCenterId,
                Name = ps.Product.ProductName ?? ps.Product.Code,
                Unit = ps.UnitMeasure.Symbol
            })
            .ToListAsync(ct);

        var validByBranch = stocks.Where(s => s.BranchId == branchId).ToList();

        if (validByBranch.Count != stockIds.Count)
            return _errorManager.ThrowBadRequest<string>("Uno o más insumos no existen.", "ERP:PRODUCT_STOCK_NOT_FOUND");

        var validStocks = validByBranch.Where(s => s.CostCenterId == costCenterId).ToList();

        if (validStocks.Count != stockIds.Count)
            return _errorManager.ThrowBadRequest<string>("Uno o más insumos no pertenecen a su centro de costo.", "ERP:PRODUCT_STOCK_COST_CENTER_MISMATCH");

        var inactive = validStocks
            .Where(s => !s.IsActive)
            .Select(s => s.Name)
            .ToList();

        if (inactive.Count > 0) return _errorManager.ThrowBadRequest<string>("Los siguientes insumos están inactivos.", "ERP:PRODUCT_STOCK_INACTIVE");

        var empty = validStocks
            .Where(s => s.AvailableQuantity <= 0)
            .Select(s => s.Name)
            .ToList();

        if (empty.Count > 0)
            return _errorManager.ThrowBadRequest<string>(
                $"Los siguientes insumos no tienen stock disponible: {string.Join(", ", empty)}.",
                "ERP:PRODUCT_STOCK_EMPTY");

        var insufficient = validStocks
            .Where(s => requested[s.Id] > s.AvailableQuantity)
            .Select(s => $"{s.Name} (solicitado: {requested[s.Id]} {s.Unit}, disponible: {s.AvailableQuantity} {s.Unit}")
            .ToList();

        if (insufficient.Count > 0)
            return _errorManager.ThrowBadRequest<string>(
                $"La cantidad solicitada supera el stock disponible: {string.Join("; ", insufficient)}.",
                "ERP:INSUFFICIENT_STOCK");

        return null;
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
}