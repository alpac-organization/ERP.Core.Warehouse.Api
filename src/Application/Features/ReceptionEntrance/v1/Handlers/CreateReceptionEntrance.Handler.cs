using MediatR;
using System.Text.Json;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Application.Commons.Interfaces.AWS;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ERP.Core.Database.Domain.Entities.Operations;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class CreateReceptionEntranceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IS3StorageService _s3Services, ICodeGenerator _codeGenerator) : BaseValidatorHandler<CreateReceptionEntranceCommand, Unit>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions SnakeCaseOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public override async Task<Unit> Handle(CreateReceptionEntranceCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tienes acceso a realizar esta acción","ERP:INVALID_ACCESS");
            }

            //Designar centro de costo de (PO)
            var receptionEntranceEntity = ReceptionEntranceMapper.ToReceptionEntranceEntity(request);
            receptionEntranceEntity.CreatedByUserId = access.User.Id;

            var (IsSuccess, Code) = await _codeGenerator.GenerateUniqueReceptionEntranceCodeAsync(cancellationToken);

            if (!IsSuccess)
            {
                return _errorManager.ThrowInternalError<Unit>("Ocurrio un error al generar codigo de recepción", "ERP:CODE_GENERATOR_ERROR");
            }

            receptionEntranceEntity.ReceptionCode = Code;

            //Manejar  el control de  pruebas de imagenes.
            AdditionalReceptionEntranceData additionalData = new();

            foreach (var image in request.EvidenceBase64)
            {
                var url = await _s3Services.UploadImageAsync("Warehouse", "ReceptionEntrance", image, cancellationToken);
                var imageEntity = ReceptionEntranceMapper.ToImagesInformation(url);

                //Agregamos a la lista la url 
                additionalData.EvidenceUrls.Add(imageEntity);
            }

            RegisterDocumentsReception(request, additionalData);

            receptionEntranceEntity.AdditionalData = JsonSerializer.Serialize(additionalData, SnakeCaseOptions);

            //Registro de información de recepción.
            await _unitOfWork.ReceptionEntrance.InsertReceptionEntrance(receptionEntranceEntity);

            //Registro de información de transporte.
            var receptionTransportInfoEntity = ReceptionEntranceMapper.ToTransportEntranceEntity(request, receptionEntranceEntity.Id);
            await _unitOfWork.ReceptionTransportEntrance.RegisterTransport(receptionTransportInfoEntity);

            switch (request.GeneralInformation.DocumentType)
            {
                case DocumentType.DUCA:
                {
                    // PO - Por cada número Duca o declaración aduanera.

                    foreach(var duca in request.GeneralInformation.DucatNumbers)
                    {
                        //Manejo de  información de (PO)
                        var operationOrderEntity = OperationalOrderMapper.ToOperationalOrderEntity(request, access.Profile.CostCenterId);

                        operationOrderEntity.DocumentNumber = duca;                    
                        operationOrderEntity.ReceptionId = receptionEntranceEntity.Id;
                        
                        var (IsSucceded, PoCode) = await _codeGenerator.GenerateUniqueOperationalOrderCodeAsync();
                        
                        if (!IsSucceded)
                        {
                            return _errorManager.ThrowInternalError<Unit>("Ocurrio un error al generar la generación de archivo", "ERP:INTERNAL_ERROR");
                        }

                        operationOrderEntity.PoCode = PoCode;

                        await _unitOfWork.OperationalOrders.RegisterOperationalOrder(operationOrderEntity);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);

                        var documentEntry = additionalData.DocumentNumbers.FirstOrDefault(
                            document => document.DocumentNumbers == duca
                                && document.OperationalOrderId == Guid.Empty);

                        if (documentEntry is not null)
                        {
                            documentEntry.OperationalOrderId = operationOrderEntity.Id;
                        }
                    }

                    break;
                }
                case DocumentType.CustomsDeclaration:
                {
                    //Manejo de  información de (PO)
                    var operationOrderEntity = OperationalOrderMapper.ToOperationalOrderEntity(request, access.Profile.CostCenterId);
                    operationOrderEntity.ReceptionId = receptionEntranceEntity.Id;

                    AssignmentOperational? assignmentOperational = null; 

                    if (request.CustomsDeclarationInformation is not null)
                    {
                        assignmentOperational = ReceptionEntranceMapper.FromReceptionToAssignmentOperationalEntity(request.CustomsDeclarationInformation);
   
                        //Verifiquemos si esta fuera de horario de ventanilla para poder dejarlo insertar esta información.     
                        if (!IsWithinAllowedCustomsWindow(DateTime.Now.TimeOfDay))
                        {
                            return _errorManager.ThrowBadRequest<Unit>(
                                "La información de declaración de aduana solo puede registrarse de 5:00 pm a 8:00 am o de 12:00 pm a 1:00 pm",
                                "ERP:CUSTOMS_DECLARATION_OUT_OF_WINDOW"
                            );
                        }
    
                        operationOrderEntity.Weight = request.CustomsDeclarationInformation.TotalWeight;
                        operationOrderEntity.PackagesCount = request.CustomsDeclarationInformation.PackageNumber;                        
                    }

                    operationOrderEntity.DocumentNumber = request.GeneralInformation.CustomsDeclarationNumber;

                    var (IsSucceded, PoCode) = await _codeGenerator.GenerateUniqueOperationalOrderCodeAsync();
                        
                    if (!IsSucceded)
                    {
                        return _errorManager.ThrowInternalError<Unit>("Ocurrio un error al generar la generación de archivo", "ERP:INTERNAL_ERROR");
                    }

                    operationOrderEntity.PoCode = PoCode;
                    
                    await _unitOfWork.OperationalOrders.RegisterOperationalOrder(operationOrderEntity);

                    if (assignmentOperational is not null)
                    {
                        operationOrderEntity.HasAssignmentOperationalActive = true;
                        assignmentOperational.OperationalOrderId = operationOrderEntity.Id;
                        await _unitOfWork.AssignmentOperationals.RegisterAssignmentOperational(assignmentOperational);   
                    }

                    var documentEntry = additionalData.DocumentNumbers.FirstOrDefault(
                        document => document.DocumentNumbers == operationOrderEntity.DocumentNumber);

                    if (documentEntry is not null)
                    {
                        documentEntry.OperationalOrderId = operationOrderEntity.Id;
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    break;   
                }
                default:
                {
                    return _errorManager.ThrowBadRequest<Unit>("Error al registrar la información, el tipo de documento no es aceptable", "ERP:INVALID_DOCUMENT");    
                }
            }

            receptionEntranceEntity.AdditionalData = JsonSerializer.Serialize(additionalData, SnakeCaseOptions);

            await _unitOfWork.ReceptionEntrance.UpdateAsync(receptionEntranceEntity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        private static void RegisterDocumentsReception(CreateReceptionEntranceCommand request, AdditionalReceptionEntranceData additionalData)
        {
            switch (request.GeneralInformation.DocumentType)
            {
                case DocumentType.DUCA:
                {
                    foreach (var document in request.GeneralInformation.DucatNumbers)
                    {
                        var documentInformation = new DocumentInformation()
                        {
                            DocumentId = Guid.NewGuid(),     
                            DocumentNumbers = document,
                            DocumentType = DocumentType.DUCA        
                        };

                        additionalData.DocumentNumbers.Add(documentInformation);
                    }

                    break;   
                }
                case DocumentType.CustomsDeclaration:
                {
                    var documentInformation = new DocumentInformation()
                    {
                        DocumentId = Guid.NewGuid(),     
                        DocumentType = DocumentType.CustomsDeclaration,
                        DocumentNumbers = request.GeneralInformation.CustomsDeclarationNumber
                    };

                    additionalData.DocumentNumbers.Add(documentInformation);

                    break;   
                }

                default:
                {
                    break;   
                }
            }
        }

        private static bool IsWithinAllowedCustomsWindow(TimeSpan currentTime)
        {
            // Ventana 1: 5:00 pm - 8:00 am (cruza medianoche)
            var overnightStart = new TimeSpan(17, 0, 0);
            var overnightEnd = new TimeSpan(8, 0, 0);
            var isInOvernightWindow = currentTime >= overnightStart || currentTime <= overnightEnd;

            // Ventana 2: 12:00 pm - 1:00 pm
            var middayStart = new TimeSpan(12, 0, 0);
            var middayEnd = new TimeSpan(13, 0, 0);
            var isInMiddayWindow = currentTime >= middayStart && currentTime <= middayEnd;

            return isInOvernightWindow || isInMiddayWindow;
        }
    }
}