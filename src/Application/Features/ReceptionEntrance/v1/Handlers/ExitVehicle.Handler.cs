using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class ExitVehicleHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<ExitVehicleCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(ExitVehicleCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 1. Busqueda del registro (SIN AsNoTracking porque se va a modificar)
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
        #endregion

        #region 2. Validar que no tenga salida ya registrada
        // if (recordEntrance.ReceptionEntrance.TransportUnitExitDate != null)
        //     return _errorManager.ThrowBadRequest<bool>(
        //         "Este vehículo ya tiene una salida registrada.",
        //         "ERP:VEHICLE_EXIT_ALREADY_REGISTERED");
        #endregion

        #region 3. Registrar salida
        var nowNica = NicaraguaClock.Now;

        // recordEntrance.ReceptionEntrance.TransportUnitExitDate = request.ExitDate ?? DateOnly.FromDateTime(nowNica);
        // recordEntrance.ReceptionEntrance.TransportUnitExitTime = request.ExitTime ?? TimeOnly.FromDateTime(nowNica);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        #endregion

        return true;
    }
}