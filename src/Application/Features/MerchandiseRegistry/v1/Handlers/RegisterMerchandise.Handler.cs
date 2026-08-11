using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterMerchandiseHandler(IUnitOfWork unitOfWork, ILogger<RegisterMerchandiseHandler> logger, IErrorManager errorManager)
    : BaseValidatorHandler<RegisterMerchandiseCommand, Guid>(unitOfWork, errorManager)
{
    public override async Task<Guid> Handle(RegisterMerchandiseCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode!,
            cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse;

        var categoryExists = await _unitOfWork.CategoryProducts.Entities
            .AnyAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken);
        
        if(!categoryExists)
            return _errorManager.ThrowBadRequest<Guid>(
                "La categoría seleccionada no existe o no está activa.",
                "ERP:003");

        logger.LogInformation("🚀 Iniciando registro de mercadería: {MerchandiseName}", request.MerchandiseName);

        var merchandiseEntity = new Merchandises
        {
            MerchandiseName = request.MerchandiseName,
            Description = request.Desciption,
            CategoryId = request.CategoryId
        };

        await _unitOfWork.Merchandises.InsertMerchandise(merchandiseEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("✅ Mercadería {MerchandiseId} registrada exitosamente", merchandiseEntity.Id);

        return merchandiseEntity.Id;
    }
}