// using System.Text.Json;
// using Microsoft.Extensions.Logging;
// using ERP.Core.Application.Commons.Interfaces;

// using ERP.Core.Database.Domain.Enums;
// using ERP.Core.Database.Application.Commons.Interfaces.Bases;
// using ERP.Core.Database.Application.Commons.Interfaces.Services;
// using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

// using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
// using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Commands;

// namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Handlers
// {
//     public class RegisterQuoteHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, ILogger<RegisterQuoteHandler> _logger) :  BaseValidatorHandler<RegisterQuoteCommand, bool>(_unitOfWork, _errorManager)
//     {
//         private static readonly JsonSerializerOptions LoggingJsonOptions = new()
//         {
//             WriteIndented = true
//         };

//         public override async Task<bool> Handle(RegisterQuoteCommand request, CancellationToken cancellationToken)
//         {
//             var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

//             if (!access.IsSuccess)
//             {
//                 return access.ErrorResponse;
//             }

//             if (access.Role?.RoleType == RoleType.Supervisor)
//             {
//                 return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
//             }

//             _logger.LogInformation("🚀Iniciando proceso de registro cotización al sistema.");

//             var (isSucceded, quotationsCode) = await _codeGenerator.GenerateUniqueCodeToQuotes(request.BranchId);

//             if (isSucceded is false)
//             {
//                 return _errorManager.ThrowBadRequest<bool>("Ocurrio un error al generar codigo de cotización", "ERP:CODE_GENERATOR");
//             }

//             //Falta asociar la orden de compra
//             var quotationEntity = QuotationMapper.ToQuotationEntity(request, access.User.Fullname ?? "", quotationsCode);
//             await _unitOfWork.Quotations.RegisterQuotation(quotationEntity);

//             foreach (var detail in request.QuoteDetails)
//             {
//                 //✅Registrar detalles de la cotización padre.
//                 var quoteDetailEntity = QuotationMapper.ToQuotationDetailEntity(detail, quotationEntity.Id);
//                 await _unitOfWork.QuotesDetails.RegisterQuoteDetail(quoteDetailEntity);

//                 // foreach (var productInformation in detail.Products)
//                 // {
//                 //     //✅Registrar detalles de productos registrados


//                 //     await _unitOfWork.QuotedProducts.RegisterQuotedProduct(new (){});
//                 // }
//             }

//             await _unitOfWork.SaveChangesAsync(cancellationToken);

//             _logger.LogInformation("✅Se registro exitosamente la cotización");
//             return true;
//         }
//     }
// }