using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class CreateReceptionEntranceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<CreateReceptionEntranceCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(CreateReceptionEntranceCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
        {
            return access.ErrorResponse!;
        }

        //Designar centro de costo de (PO)
        var receptionEntranceEntity = ReceptionEntranceMapper.ToReceptionEntranceEntity(request);

        var operationOrderEntity = OperationalOrderMapper.ToOperationalOrderEntity(access.Profile.CostCenterId);     
        operationOrderEntity.ReceptionId = receptionEntranceEntity.Id;   


        //Registro de información de recepción.
        await _unitOfWork.ReceptionEntrance.InsertReceptionEntrance(receptionEntranceEntity);

        switch (request.GeneralInformation.DocumentType)
        {
            case DocumentType.DUCA:
            {
                // PO - Por cada número Duca o declaración aduanera.
                foreach(var duca in request.GeneralInformation.DucatNumbers)
                {
                    operationOrderEntity.DucaNumber = duca;                    
                    await _unitOfWork.OperationalOrders.RegisterOperationalOrder(operationOrderEntity);
                }

                break;
            }
            case DocumentType.CustomsDeclaration:
            {
                operationOrderEntity.DucaNumber = request.GeneralInformation.CustomsDeclarationNumber;

                await _unitOfWork.OperationalOrders.RegisterOperationalOrder(operationOrderEntity);
                break;   
            }
            default:
            {
                return _errorManager.ThrowBadRequest<Unit>("Error al registrar la información, el tipo de documento no es aceptable", "ERP:INVALID_DOCUMENT");    
            }
        }

        //Registro de información de transporte.
        var receptionTransportInfoEntity = ReceptionEntranceMapper.ToTransportEntranceEntity(request, receptionEntranceEntity.Id);
        await _unitOfWork.ReceptionTransportEntrance.RegisterTransport(receptionTransportInfoEntity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}