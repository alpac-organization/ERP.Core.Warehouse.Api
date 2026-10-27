using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Handlers;

public class RegisterSupplyRequestsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ICodeGenerator codeGenerator)
    : BaseValidatorHandler<RegisterSupplyRequestsCommad, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(RegisterSupplyRequestsCommad request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        // Validar que la PO exista
        var orderExists = await _unitOfWork.OperationalOrders.Entities
            .AsNoTracking()
            .AnyAsync(op => op.Id == request.OperationalOrderId && op.DeletedAt == null, ct);
            

        if (!orderExists) return _errorManager.ThrowBadRequest<Unit>("La Orden Operativa no existe.", "ERP:OPERATIVE_ORDER_NOT_FOUND");

        var stockIds = request.Items.Select(i => i.ProductStockId).ToList();

        var existingStocks = await _unitOfWork.ProductStocks.Entities
            .AsNoTracking()
            .CountAsync(ps => stockIds.Contains(ps.Id) && ps.DeletedAt == null, ct);

        if (existingStocks != stockIds.Count) return _errorManager.ThrowBadRequest<Unit>("Uno o más insumos no existen.","ERP:PRODUCT_STOCK_NOT_FOUND");
        
        var warehouseId = await _unitOfWork.AssignmentOperationals.Entities
            .AsNoTracking()
            .Where(a => a.OperationalOrderId == request.OperationalOrderId
                    && a.IsActive && a.DeletedAt == null)
            .Select(a => a.WarehouseId!.Value)
            .FirstAsync(ct);

        var branchCode = access.Profile.Branch.BranchCode;

        if(branchCode is null) return _errorManager.ThrowBadRequest<Unit>("No se encontró el código  de sucursal.", "ERP:BRANCH_CODE_NOT_FOUND");

        var (ok, code) = await codeGenerator.GenerateCodeToSupplyRequests(branchCode, ReasonUse.Operation, ct);

        var entity = request.ToSupplyRequestEntity(access.Profile, code, warehouseId);
        
        await _unitOfWork.SupplyRequests.RegisterSupplyRequest(entity);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;

    }
}