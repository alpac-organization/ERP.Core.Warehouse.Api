using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

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
            .Include(r => r.EntranceDucats.Where(d => d.DeletedAt == null))
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        #region 1. sincronizacion de Ducas
        if (request.Ducats != null)
        {
            var normalizedItems = request.Ducats
                .Select(d => new { d.Id, Number = d.DucatNumber.Trim().Replace(" ", "") })
                .ToList();

            #region 1a. Confirmar Id presente en los DUCA
            var itemsWithoutId = normalizedItems.Where(d => !d.Id.HasValue).ToList();

            if (itemsWithoutId.Count != 0)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    "No se permite crear nuevos DUCA's en esta operación.",
                    "ERP:DUCA_INSERT_NOT_ALLOWED");
            }
            #endregion

            #region 1b. Duplicados en la misma solicitud
            var duplicatesInRequest = normalizedItems
                .GroupBy(d => d.Number, StringComparer.OrdinalIgnoreCase)
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

            #region 1c. Unicidad global, excluyendo los ducas que ya pertenecen al registro actual
            var normalizedNumbersLower = normalizedItems
                .Select(d => d.Number.ToLower())
                .ToList();

            var duplicateGlobalDucas = await _unitOfWork.EntranceDucats.Entities
                .Where(d => d.RecordEntranceId != request.ReceptionId &&
                            d.DeletedAt == null &&
                            normalizedNumbersLower.Contains(d.DucatNumber.Trim().ToLower()))
                .Select(d => d.DucatNumber)
                .ToListAsync(cancellationToken);

            if (duplicateGlobalDucas.Count != 0)
            {
                return _errorManager.ThrowBadRequest<bool>(
                    $"Los siguientes números DUCA ya están registrados en el sistema: {string.Join(", ", duplicateGlobalDucas)}.",
                    "ERP:GLOBAL_DUPLICATED_DUCA_ERROR");
            }
            #endregion

            #region 1d. Validar que los ducas no tengas registros hijos
            var ducatIdsToUpdate = normalizedItems
                .Select(i => i.Id!.Value)
                .ToList();
            
            var ducatsWithChildren = await _unitOfWork.EntranceDucats.Entities
                .Where(d => ducatIdsToUpdate.Contains(d.Id) && d.DeletedAt == null)
                .Where(d => d.Discrepancy != null || d.RegistryDetail != null)
                .Select(d => d.DucatNumber)
                .ToListAsync(cancellationToken);

            if (ducatsWithChildren.Count != 0)
            {
                return _errorManager.ThrowBadRequest<bool> (
                    $"Las siguientes DUCA's tienen registros relacionados y no pueden editarse: {string.Join(", ", ducatsWithChildren)}",
                    "ERP:DUCA_HAS_RELATED_RECORDS");
            }
            #endregion

            #region  1e. Actualizar por Id
            var existingDucats = recordEntrance.EntranceDucats.ToList();

            foreach (var item in normalizedItems)
            {
                Guid ducatId = item.Id!.Value;
                
                var ducat = existingDucats.FirstOrDefault(d => d.Id == ducatId);

                if (ducat == null)
                {
                    return _errorManager.ThrowBadRequest<bool>(
                        $"La DUCA con id '{item.Id}' no pertenece a este registro de recepción.",
                        "ERP:DUCA_NOT_FOUND");
                }

                ducat.DucatNumber = item.Number;

            }
            #endregion
        }
        #endregion

        #region 2. Actualizacion parcial de los campos de la recepcion
        if (recordEntrance.ReceptionEntrance != null)
        {
            var reception = recordEntrance.ReceptionEntrance;

            if (request.CountryOfOrigin != null) reception.CountryOfOrigin = request.CountryOfOrigin;
            if (request.Aduana != null) reception.Aduana = request.Aduana;
            if (request.PlateNumber != null) reception.PlateNumber = request.PlateNumber;
            if (request.TrailerChassis != null) reception.TrailerChassis = request.TrailerChassis;
            if (request.DriverLicense != null) reception.DriverLicense = request.DriverLicense;
            if (request.Transportista != null) reception.Transportista = request.Transportista;
            if (request.Medio != null) reception.Medio = request.Medio;
            if (request.DriverName != null) reception.DriverName = request.DriverName;
            if (request.Consignee != null) reception.Consignee = request.Consignee;
            if (request.SealNumber != null) reception.SealNumber = request.SealNumber;

            var user = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            var nowNica = NicaraguaClock.Now;

            reception.UpdatedByUserId = request.UserId.ToString();
            reception.UpdatedByUserName = user?.Fullname ?? user?.UserName ?? request.UserId.ToString();
            reception.UpdatedDate = DateOnly.FromDateTime(nowNica);
            reception.UpdatedTime = TimeOnly.FromDateTime(nowNica);
        }
        #endregion

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;

    }
}