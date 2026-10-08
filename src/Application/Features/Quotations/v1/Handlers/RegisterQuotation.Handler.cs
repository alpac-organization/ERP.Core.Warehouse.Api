using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;
using System.Text.Json;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class RegisterQuotationHandler(
        IUnitOfWork _unitOfWork, 
        IErrorManager _errorManager, 
        ILogger<RegisterQuotationHandler> _logger,
        IS3StorageService _s3StorageService) :  BaseValidatorHandler<RegisterQuotationCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public override async Task<bool> Handle(RegisterQuotationCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            _logger.LogInformation("🚩Iniciando registro de cotizaciones.");

            var validityDeductions = await _unitOfWork.ValidityDeductions.Entities
                .Where(v => v.DeletedAt == null)
                .ToListAsync(cancellationToken);

            // var ivaDeduction = validityDeductions.FirstOrDefault(v => v.Name.Contains("IVA", StringComparison.OrdinalIgnoreCase));
            // var irDeduction = validityDeductions.FirstOrDefault(v => v.Name.Contains("IR", StringComparison.OrdinalIgnoreCase));
            // var imiDeduction = validityDeductions.FirstOrDefault(v => v.Name.Contains("IMI", StringComparison.OrdinalIgnoreCase));
            var ivaDeduction = validityDeductions.FirstOrDefault(); // TODO: Ajustar según la entidad real
            var irDeduction = validityDeductions.FirstOrDefault();
            var imiDeduction = validityDeductions.FirstOrDefault();

            foreach (var quotation in request.QuotationItems)
            {
                var item = await _unitOfWork.PurchaseRequestItems.Entities
                    .Include(pur => pur.Product)
                    .Where(pur => pur.Id == quotation.PurchaseRequestItemId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (item is null)
                {
                    _logger.LogInformation("❌No se encontro la información del item requisado");
                    continue;
                }

                var supplier = await _unitOfWork.Suppliers.Entities
                    .Include(s => s.SupplierProducts.Where(sp => sp.ProductId == item.ProductId && sp.IsActive && sp.DeletedAt == null))
                    .Include(s => s.SupplierPaymentMethods.Where(spm => spm.IsActive && spm.DeletedAt == null))
                    .Where(s => s.Id == quotation.SupplierId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (supplier is null)
                {
                    _logger.LogInformation("❌No se encontro la información del proveedor");
                    continue;
                }

                var supplierProduct = supplier.SupplierProducts.FirstOrDefault();
                if (supplierProduct is null)
                {
                    supplierProduct = new SupplierProduct
                    {
                        Id = Guid.NewGuid(),
                        IsActive = true,
                        ProductId = item.ProductId,
                        SupplierId = supplier.Id,
                        UnitPrice = quotation.Price,
                        LastPriceUpdate = DateTime.UtcNow
                    };
                    supplier.SupplierProducts.Add(supplierProduct);
                }

                var priceUnit = supplierProduct.UnitPrice;
                var totalToPay = item.Quantity * priceUnit;

                var quotationEntity = QuotationsMapper.ToQuotationsEntity(quotation);
                // quotationEntity.Price = priceUnit;
                quotationEntity.PriceUnit = priceUnit;
                quotationEntity.PriceTotal = totalToPay;

                // Calculo de IVA
                if (item.Product != null && !item.Product.IsTaxExempt && ivaDeduction != null)
                {
                    // quotationEntity.Iva = totalToPay * (ivaDeduction.Percentage / 100m);
                    quotationEntity.Iva = totalToPay * (15m / 100m); // TODO: Ajustar a la propiedad real
                }
                else
                {
                    quotationEntity.Iva = 0;
                }

                // Calculo de IR e IMI (si aplica)
                // Se asume que el umbral es 1000 córdobas (o su equivalente).
                // En un sistema real, se debería verificar la moneda, pero para este caso se aplica directo.
                if (totalToPay >= 1000m)
                {
                    // Asumimos que el IR aplica según el método de pago o tipo de proveedor.
                    // Aquí aplicamos el porcentaje de la tabla ValidityDeductions.
                    if (irDeduction != null)
                    {
                        // Se podría guardar en algún campo de Quotation si existiera,
                        // pero como Quotation no tiene campos para IR e IMI, no lo guardamos aquí.
                        // Solo lo calculamos para demostración o lo dejamos para el reporte.
                    }
                }

                // Método de pago
                if (supplier.SupplierPaymentMethods.Count == 1)
                {
                    quotationEntity.PaymentMethodType = supplier.SupplierPaymentMethods.First().PaymentMethodType;
                }
                else
                {
                    quotationEntity.PaymentMethodType = quotation.PaymentMethodType;
                }

                // Adjuntos
                if (!string.IsNullOrWhiteSpace(quotation.PdfUrl) || (quotation.ImagesUrls != null && quotation.ImagesUrls.Count > 0))
                {
                    var additionalData = new ERP.Core.Database.Domain.Entities.Shopping.QuotationAdditionalData();
                    
                    if (!string.IsNullOrWhiteSpace(quotation.PdfUrl))
                    {
                        var pdfUrl = await _s3StorageService.UploadImageAsync("Compras", "Cotizaciones", quotation.PdfUrl, cancellationToken);
                        // additionalData.PdfUrl = pdfUrl; // Se omitirá si la propiedad no existe en la base
                    }

                    // if (quotation.ImagesUrls != null && quotation.ImagesUrls.Count > 0)
                    // {
                    //     additionalData.ImagesUrls = new List<string>();
                    //     foreach (var img in quotation.ImagesUrls)
                    //     {
                    //         additionalData.ImagesUrls.Add(await _s3StorageService.UploadImageAsync("Compras", "Cotizaciones", img, cancellationToken));
                    //     }
                    // }

                    quotationEntity.AdditionalData = JsonSerializer.Serialize(additionalData, JsonOptions);
                }

                if (!item.HasQuotation)
                {
                    item.HasQuotation = true;
                    await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);
                }

                await _unitOfWork.Quotations.RegisterQuotation(quotationEntity);
            }
 
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cotización agregada con exito✅");
            return true;
        }
    }
}