using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class UpdateReceptionEntranceHandler(
    IUnitOfWork _unitOfWork,
    IErrorManager _errorManager)
    : IRequestHandler<UpdateReceptionEntranceCommand, bool>
{
    public async Task<bool> Handle(UpdateReceptionEntranceCommand request, CancellationToken cancellationToken)
    {
        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance)
            .Include(r => r.EntranceDucats)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);
        
        if(recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        #region 1. Validacion DUCA en la misma peticion
        var duplicatesInRequest = request.DucatNumbers
            .GroupBy(d => d.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        
        if (duplicatesInRequest.Count != 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"La lista de DUCA's contiene números duplicados en la misma solicitud: {string.Join(", ", duplicatesInRequest)}",
                "ERP:REQUEST_DUPLICATED_DUCA");
        }
        #endregion

        #region 2. Validacion DUCA unicidad global
        var normalizedRequestDucas = request.DucatNumbers
            .Select(d => d.Trim().ToLower())
            .ToList();

        var duplicateGlobalducas = await _unitOfWork.EntranceDucats.Entities
            .Where(d => d.RecordEntranceId != request.ReceptionId &&
                    d.DeletedAt == null &&
                    normalizedRequestDucas.Contains(d.DucatNumber.Trim().ToLower()))
            .Select(d => d.DucatNumber)
            .ToListAsync(cancellationToken);
        
        if(duplicateGlobalducas.Count != 0)
        {
            return _errorManager.ThrowBadRequest<bool>(
                $"Los siguientes números DUCA ya están registrados en el sistema: {string.Join(", ", duplicateGlobalducas)}.",
                "ERP:GLOBAL_DUPLICATED_DUCA_ERROR");
        }
        #endregion

        #region  3. Actualizacion de campos
        var nowNica = NicaraguaClock.Now;
        var todayNica = DateOnly.FromDateTime(nowNica);
        var timeNica = TimeOnly.FromDateTime(nowNica);

        if(recordEntrance.ReceptionEntrance != null)
        {
            recordEntrance.ReceptionEntrance.CountryOfOrigin = request.CountryOfOrigin;
            recordEntrance.ReceptionEntrance.Aduana = request.Aduana;
            recordEntrance.ReceptionEntrance.PlateNumber = request.PlateNumber;
            recordEntrance.ReceptionEntrance.TrailerChassis = request.TrailerChassis;
            recordEntrance.ReceptionEntrance.DriverLicense = request.DriverLicense;
            recordEntrance.ReceptionEntrance.Transportista = request.Transportista;
            recordEntrance.ReceptionEntrance.Medio = request.Medio;
            recordEntrance.ReceptionEntrance.DriverName = request.DriverName;
            recordEntrance.ReceptionEntrance.Consignee = request.Consignee;
            recordEntrance.ReceptionEntrance.SealNumber = request.SealNumber;

            recordEntrance.ReceptionEntrance.UpdatedByUserId = request.UserId.ToString();
            recordEntrance.ReceptionEntrance.UpdatedDate = todayNica;
            recordEntrance.ReceptionEntrance.UpdatedTime = timeNica;
        }

        recordEntrance.IsConsolidated = request.DucatNumbers.Count > 1;

        #endregion

        #region 4. Sincronizacion de Ducas
        var existingDucas = recordEntrance.EntranceDucats.ToList();
        var newDucatNumbersNormalized = request.DucatNumbers
            .Select(d => d.Trim().Replace(" ", ""))
            .ToList();

        //Eliminar Duca de la lista
        foreach (var existingDucat in existingDucas)
        {
            if (!newDucatNumbersNormalized.Contains(existingDucat.DucatNumber, StringComparer.OrdinalIgnoreCase))
            {
                recordEntrance.EntranceDucats.Remove(existingDucat);
            }
        }

        //Agregar nuevas ducas
        foreach (var ducatNum in newDucatNumbersNormalized)
        {
            var exists = existingDucas.Any(e => e.DucatNumber.Equals(ducatNum, StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                var newDucatentity = ducatNum.ToEntranceDucaEntity(recordEntrance.Id);
                recordEntrance.EntranceDucats.Add(newDucatentity);
            }
        }
        #endregion

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
        
    }
}