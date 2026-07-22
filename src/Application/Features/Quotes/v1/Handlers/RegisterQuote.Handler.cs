using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Commands;
using System.Text.Json;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Handlers
{
    public class RegisterQuoteHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, ILogger<RegisterQuoteHandler> _logger) :  BaseValidatorHandler<RegisterQuoteCommand, bool>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions LoggingJsonOptions = new()
        {
            WriteIndented = true
        };

        public override async Task<bool> Handle(RegisterQuoteCommand request, CancellationToken cancellationToken)
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

            _logger.LogInformation("🚀Iniciando proceso de registro cotización al sistema.");

            //Generar codigo de cotización

            var (isSucceded, quotationsCode) = await _codeGenerator.GenerateUniqueCodeToQuotes(request.BranchId);

            if (isSucceded is false)
            {
                return _errorManager.ThrowBadRequest<bool>("Ocurrio un error al generar codigo de cotización", "ERP:CODE_GENERATOR");
            }

            var quotationEntity = QuotationMapper.ToQuotationEntity(request, access.User.UserName ?? "", quotationsCode);

            await _unitOfWork.Quotations.RegisterQuotation(quotationEntity);

            //Mapeamos todos los items para registrar una cotización

            foreach (var detail in request.QuoteDetails)
            {
                var detailJson = JsonSerializer.Serialize(detail, LoggingJsonOptions);
                _logger.LogInformation("Agregar detalle: {DetailJson}", detailJson);

                var quoteDetails = QuotationMapper.ToQuoteDetailsEntity(detail);

                quoteDetails.ProductId = detail.ProductId;
                quoteDetails.SupplierId = detail.SupplierId;
                quoteDetails.QuotationId = quotationEntity.Id;
                quoteDetails.UnitMeasureId = detail.UnitMeasureId;

                if (detail?.IsNewProduct ?? false)
                {
                    var productMapped = QuotationMapper.ToProductEntity(detail?.ProductInformation ?? new());
                    await _unitOfWork.Products.InsertProduct(productMapped);

                    quoteDetails.ProductId = productMapped.Id;
                }

                if (detail?.IsNewSupplier ?? false)
                {
                    var supplierMapped = QuotationMapper.ToSupplierEntity(detail?.SupplierDatails ?? new (), access.User.Fullname ?? "");
                    await _unitOfWork.Suppliers.RegisterSupplier(supplierMapped);

                    quoteDetails.SupplierId = supplierMapped.Id;
                }

                await _unitOfWork.QuotesDetails.RegisterQuoteDetail(quoteDetails);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅Se registro exitosamente la cotización");
            return true;
        }
    }
}