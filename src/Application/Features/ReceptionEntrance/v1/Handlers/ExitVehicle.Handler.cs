using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class ExitVehicleHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<ExitVehicleCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(ExitVehicleCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 1. Búsqueda del registro (SIN AsNoTracking porque se va a modificar)
        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null
            || recordEntrance.ReceptionEntrance == null
            || recordEntrance.ReceptionEntrance.DeletedAt != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        var reception = recordEntrance.ReceptionEntrance;
        #endregion

        #region 2. Validar coherencia según el tipo de unidad de transporte
        if (reception.TransportUnit == TransportUnit.Van)
        {
            if (request.ExitContainer)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Este expediente es de tipo Furgón, no aplica salida de contenedor.",
                    "ERP:INVALID_EXIT_TARGET_FOR_FURGON");
            }

            if (!request.ExitVehicle)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Debe indicar la salida del vehículo.",
                    "ERP:EXIT_TARGET_REQUIRED");
            }
        }
        else // Contenedor
        {
            if (!request.ExitVehicle && !request.ExitContainer)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "Para expedientes de tipo Contenedor debe indicar si la salida es de Vehículo, Contenedor o ambos.",
                    "ERP:EXIT_TARGET_REQUIRED");
            }
        }
        #endregion

        #region 3. Validar que no tenga salida ya registrada
        if (request.ExitVehicle && reception.VehicleExitDate != null && reception.VehicleExitTime != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Este vehículo ya tiene una salida registrada.",
                "ERP:VEHICLE_EXIT_ALREADY_REGISTERED");
        }

        if (request.ExitContainer && reception.ContainerExitDate != null && reception.ContainerExitTime != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "Este contenedor ya tiene una salida registrada.",
                "ERP:CONTAINER_EXIT_ALREADY_REGISTERED");
        }
        #endregion

        #region 4. Registrar salida
        var exitDate = request.ExitDate ?? NicaraguaClock.Today;
        var exitTime = request.ExitTime ?? NicaraguaClock.TimeNow;

        if (request.ExitVehicle) reception.ApplyVehicleExit(exitDate, exitTime);
        if (request.ExitContainer) reception.ApplyContainerExit(exitDate, exitTime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return true;
    }
}