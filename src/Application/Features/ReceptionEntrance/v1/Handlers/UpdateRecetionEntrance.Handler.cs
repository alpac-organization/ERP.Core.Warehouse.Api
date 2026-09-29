using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class UpdateReceptionEntranceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<UpdateReceptionEntranceCommand, Unit>(_unitOfWork, _errorManager)
    {
        public override async Task<Unit> Handle(UpdateReceptionEntranceCommand request, CancellationToken cancellationToken)
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

            var receptionEntrance = await _unitOfWork.ReceptionEntrance.Entities
                .Where(reception => reception.IsActive)
                .Where(reception => reception.Id == request.ReceptionEntranceId)
                .FirstOrDefaultAsync(cancellationToken);

            if (receptionEntrance is null)
            {
                return _errorManager.ThrowNotFound<Unit>("La reception a actualizar no existe registrada", "ERP:NOT_FOUND_RECEPTION");
            }

            if (request.GeneralInformation is not null)
            {
                receptionEntrance.SealNumber = request.GeneralInformation?.SealNumber ?? receptionEntrance.SealNumber;
                receptionEntrance.CountryOfOrigin = request.GeneralInformation?.CountryOrigin ?? receptionEntrance.CountryOfOrigin;
                receptionEntrance.ContainerNumber = request.GeneralInformation?.ContainerNumber ?? receptionEntrance.ContainerNumber;

                if (request.GeneralInformation?.CustomBranchId is not null)
                {
                    var customBranch = await _unitOfWork.CustomerBranches.Entities
                        .Where(cb => cb.IsActive)
                        .Where(cb => cb.Id == request.GeneralInformation.CustomBranchId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (customBranch is null)
                    {
                        return _errorManager.ThrowBadRequest<Unit>("La aduana de procendencia que desea actualizar no esta registrada", "ERP:ERROR_CUSTOM_BRANCH");
                    }

                    receptionEntrance.CustomBranchId = request.GeneralInformation.CustomBranchId;
                }        
                
                await _unitOfWork.ReceptionEntrance.UpdateAsync(receptionEntrance);
            }

            if (request.ReceptionTransportInformation is not null)
            {

                var receptionTransportEntrance = await _unitOfWork.ReceptionTransportEntrance.Entities
                    .Where(reception => reception.ReceptionEntranceId == receptionEntrance.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (receptionTransportEntrance is null)
                {
                    return _errorManager.ThrowBadRequest<Unit>("La información de transporte no se ha encontrado","ERP:ERROR_UPDATED");
                }

                receptionTransportEntrance.DriverName = request.ReceptionTransportInformation?.DriverName ?? receptionTransportEntrance.DriverName;
                receptionTransportEntrance.Transportista = request.ReceptionTransportInformation?.Transportista ?? receptionTransportEntrance.Transportista;
                receptionTransportEntrance.DriverLicense = request.ReceptionTransportInformation?.DriverLicense ?? receptionTransportEntrance.DriverLicense;
                // receptionTransportEntrance.TransportUnit = request.ReceptionTransportInformation?.TransportUnit ?? receptionTransportEntrance.TransportUnit;
                receptionTransportEntrance.VehiclePlateNumber = request.ReceptionTransportInformation?.VehiclePlateNumber ?? receptionTransportEntrance.VehiclePlateNumber;
                receptionTransportEntrance.VehicleChassisNumber = request.ReceptionTransportInformation?.VehicleChassisNumber ?? receptionTransportEntrance.VehicleChassisNumber;
            

                await _unitOfWork.ReceptionTransportEntrance.UpdateAsync(receptionTransportEntrance);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
