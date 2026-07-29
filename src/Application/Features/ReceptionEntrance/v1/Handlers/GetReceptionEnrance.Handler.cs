using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class GetReceptionEntrancesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : IRequestHandler<GetReceptionEntrancesQuery, GetReceptionEntrancesDto>
{
    public async Task<GetReceptionEntrancesDto> Handle(GetReceptionEntrancesQuery request, CancellationToken cancellationToken)
    {
        #region 1. Busqueda de Code
        var receptionStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (receptionStep == null)
        {
            return _errorManager.ThrowInternalError<GetReceptionEntrancesDto>(
                "No se encontró una configuración para el flujo de trabja (WorkflowStepDefinition). Contacte al administrador.",
                "ERP:WORKFLOW_NOT_CONFIGURED");
        }

        var receptionStepCode = receptionStep.Code;
        #endregion

        var targetDate = request.Date.HasValue
            ? DateOnly.FromDateTime(request.Date.Value)
            : NicaraguaClock.Today;

        #region 2. Stats del dia (Filtro de busqueda)
        var totalEntries = await _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate == targetDate))
            .CountAsync(cancellationToken);

        var recepcionadosQuery = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate <= targetDate));

        var totalOnSite = await recepcionadosQuery
            .Where(r => r.ReceptionEntrance == null)
            .CountAsync(cancellationToken);

        var totalExits = await recepcionadosQuery
            .Where(r => r.ReceptionEntrance != null)
            .CountAsync(cancellationToken);
        #endregion

        #region 3. Listado filtrado y paginado
        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Include(r => r.ReceptionEntrance)
            .Include(r => r.EntranceDucats.Where(d => d.DeletedAt == null))
            .Include(r => r.ExecutionLogs.Where(l => l.WorkflowStepDefinitionCode == receptionStepCode))
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate == targetDate) &&
                r.ReceptionEntrance != null &&
                r.ReceptionEntrance.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.DriverName))
        {
            var driverFilter = request.DriverName.Trim().ToLower();
            query = query.Where(r => r.ReceptionEntrance != null && r.ReceptionEntrance.DriverName.ToLower().Contains(driverFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
        {
            var plateFilter = request.PlateNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(r => r.ReceptionEntrance != null &&
                r.ReceptionEntrance.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.DucatNumber))
        {
            var ducatFilter = request.DucatNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(r => r.EntranceDucats.Any(d =>
                                                d.DucatNumber.ToLower().Replace(" ", "").Contains(ducatFilter)));
        }

        if (request.DucatId.HasValue)
        {
            query = query.Where(r => r.EntranceDucats.Any(d => d.Id == request.DucatId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        #endregion

        #region 4. Mapeo a Dto
        var data = records.Select(r =>
        {
            var receptionLog = r.ExecutionLogs.First(l => l.WorkflowStepDefinitionCode == receptionStepCode);
            int? durationTotalSeconds = null;
            string? durationFormatted = null;
            if (receptionLog.EndDate.HasValue && receptionLog.EndTime.HasValue)
            {
                var start = receptionLog.StartDate.ToDateTime(receptionLog.StartTime);
                var end = receptionLog.EndDate.Value.ToDateTime(receptionLog.EndTime.Value);
                var span = end - start;

                durationTotalSeconds = (int)span.TotalSeconds;
                durationFormatted = span.ToString(@"hh\:mm\:ss");
            }

            return new RecordEntranceItemDto
            {
                Id              = r.Id,
                Status          = r.Status,
                IsConsolidated  = r.IsConsolidated,

                ReceptionEntrance = r.ReceptionEntrance == null ? null : new ReceptionEntranceItemDto
                {
                    Id                  = r.ReceptionEntrance.Id,
                    CountryOfOrigin     = r.ReceptionEntrance.CountryOfOrigin,
                    Aduana              = r.ReceptionEntrance.Aduana,
                    PlateNumber         = r.ReceptionEntrance.PlateNumber,
                    TrailerChassis      = r.ReceptionEntrance.TrailerChassis,
                    DriverLicense       = r.ReceptionEntrance.DriverLicense,
                    Transportista       = r.ReceptionEntrance.Transportista,
                    DriverName          = r.ReceptionEntrance.DriverName,
                    SealNumber          = r.ReceptionEntrance.SealNumber,
                    UpdatedByUserName   = r.ReceptionEntrance.UpdatedByUserName,
                    UpdatedDate         = r.ReceptionEntrance.UpdatedDate,
                    UpdatedTime         = r.ReceptionEntrance.UpdatedTime,
                },

                ExecutionLog = new StepExecutionLogItemDto
                {
                    StartDate                   = receptionLog.StartDate,
                    StartTime                   = receptionLog.StartTime,
                    EndDate                     = receptionLog.EndDate,
                    EndTime                     = receptionLog.EndTime,
                    ProcessedByUserName         = receptionLog.ProcessedByUserName,
                    DurationTotalSeconds        = durationTotalSeconds,
                    DurationFormatted           = durationFormatted
                },

                Ducats = [.. r.EntranceDucats.Select(d => new EntranceDucatItemDto
                {
                    Id = d.Id,
                    DucatNumber = d.DucatNumber
                })]
            };
        }).ToList();
        #endregion

        var stats = new ReceptionEntranceStatsDto
        {
            TotalEntries       = totalEntries,
            TotalOnSite       = totalOnSite,
            TotalExits    = totalExits
        };

        return new GetReceptionEntrancesDto
        {
            Data        = data,
            TotalCount  = totalCount,
            PageNumber  = request.PageNumber,
            PageSize    = request.PageSize,
            Stats       = stats
        };
    }
}