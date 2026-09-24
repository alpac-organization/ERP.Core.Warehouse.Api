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

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class CreateReceptionEntranceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IS3StorageService _s3Services, ICodeGenerator _codeGenerator) : BaseValidatorHandler<CreateReceptionEntranceCommand, Unit>(_unitOfWork, _errorManager)
    {
        private static readonly (TimeSpan Start, TimeSpan End)[] AllowedCustomsWindows =
        [
            (new TimeSpan(5, 0, 0), new TimeSpan(8, 0, 0)),   // 5:00 pm - 8:00 am
            (new TimeSpan(12, 0, 0), new TimeSpan(13, 0, 0))  // 12:00 pm - 1:00 pm
        ];

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

            //Manejar  el control de  pruebas de imagenes.
            AdditionalReceptionEntranceData additionalData = new();

            foreach (var image in request.EvidenceBase64)
            {
                var url = await _s3Services.UploadImageAsync("Warehouse", "ReceptionEntrance", image, cancellationToken);
                var imageEntity = ReceptionEntranceMapper.ToImagesInformation(url);

                //Agregamos a la lista la url 
                additionalData.EvidenceUrls.Add(imageEntity);
            }

            receptionEntranceEntity.AdditionalData = JsonSerializer.Serialize(additionalData);

            //Registro de información de recepción.
            await _unitOfWork.ReceptionEntrance.InsertReceptionEntrance(receptionEntranceEntity);

            //Registro de información de transporte.
            var receptionTransportInfoEntity = ReceptionEntranceMapper.ToTransportEntranceEntity(request, receptionEntranceEntity.Id);
            await _unitOfWork.ReceptionTransportEntrance.RegisterTransport(receptionTransportInfoEntity);

            //Manejo de  información de (PO)
            var operationOrderEntity = OperationalOrderMapper.ToOperationalOrderEntity(access.Profile.CostCenterId);

            var (IsSucceded, PoCode) = await _codeGenerator.GenerateUniqueOperationalOrderCodeAsync(access.Profile.CostCenterId, cancellationToken);

            if (!IsSucceded)
            {
                return _errorManager.ThrowInternalError<Unit>("Ocurrio un error al generar la generación de archivo", "ERP:INTERNAL_ERROR");
            }
            
            operationOrderEntity.OpCode = PoCode;
            operationOrderEntity.ReceptionId = receptionEntranceEntity.Id;
            operationOrderEntity.DocumentType = request.GeneralInformation.DocumentType;

            switch (request.GeneralInformation.DocumentType)
            {
                case DocumentType.DUCA:
                {
                    // PO - Por cada número Duca o declaración aduanera.
                    foreach(var duca in request.GeneralInformation.DucatNumbers)
                    {
                        operationOrderEntity.DocumentNumber = duca;                    
                        await _unitOfWork.OperationalOrders.RegisterOperationalOrder(operationOrderEntity);
                    }

                    break;
                }
                case DocumentType.CustomsDeclaration:
                {
                    operationOrderEntity.DocumentNumber = request.GeneralInformation.CustomsDeclarationNumber;

                    if (request.CustomsDeclarationInformation is not null)
                    {
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
                        operationOrderEntity.Description = request.CustomsDeclarationInformation.ProductDescription;
                    }

                    await _unitOfWork.OperationalOrders.RegisterOperationalOrder(operationOrderEntity);
                    break;   
                }
                default:
                {
                    return _errorManager.ThrowBadRequest<Unit>("Error al registrar la información, el tipo de documento no es aceptable", "ERP:INVALID_DOCUMENT");    
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
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