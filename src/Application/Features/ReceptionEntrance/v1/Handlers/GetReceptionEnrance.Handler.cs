using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class GetReceptionEntrancesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : IRequestHandler<GetReceptionEntrancesQuery, GetReceptionEntrancesDto>
{
    public async Task<GetReceptionEntrancesDto> Handle(GetReceptionEntrancesQuery request, CancellationToken cancellationToken)
    {
        #region Busqueda de Code
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

        #region Stats del dia (Filtro de busqueda)
        var statsRaw = await _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ExecutionLogs.Any(l => 
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate == targetDate))
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var stats = new ReceptionEntranceStatsDto
        {
            InTail = statsRaw.FirstOrDefault(s => s.Status == RecordEntranceStatus.InTail)?.Count ?? 0,
            InUnloading = statsRaw.FirstOrDefault(s => s.Status == RecordEntranceStatus.InUnloading)?.Count ?? 0,
            Completed = statsRaw.FirstOrDefault(s => s.Status == RecordEntranceStatus.Completed)?.Count ?? 0
        };
        #endregion

        #region Listado filtrado y paginado
        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Include(r => r.ReceptionEntrance)
            .Include(r => r.EntranceDucats)
            .Include(r => r.ExecutionLogs.Where(l => l.WorkflowStepDefinitionCode == receptionStepCode))
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate == targetDate));

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

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        #endregion

        var data = records.Select(r => 
        {
            var receptionLog = r.ExecutionLogs.First(l => l.WorkflowStepDefinitionCode == receptionStepCode);
            int? durationTotalSeconds = null;
            string? durationFormatted = null;
            if(receptionLog.EndDate.HasValue && receptionLog.EndTime.HasValue)
            {
                var start = receptionLog.StartDate.ToDateTime(receptionLog.StartTime);
                var end = receptionLog.EndDate.Value.ToDateTime(receptionLog.EndTime.Value);
                var span = end - start;

                durationTotalSeconds = (int)span.TotalSeconds;
                durationFormatted = span.ToString(@"hh\:mm\:ss");
            }

            return new ReceptionEntranceListItemDto{
            RecordEntranceId = r.Id,
            Status = r.Status.ToString(),
            CurrentStepCode = r.CurrentStepCode,
            IsConsolidated = r.IsConsolidated,
            CreatedAt = r.CreatedAt,

            ReceptionStartDate = receptionLog.StartDate,
            ReceptionStartTime = receptionLog.StartTime,
            ReceptionEndDate = receptionLog.EndDate,
            ReceptionEndTime = receptionLog.EndTime,
            DurationTotalSeconds = durationTotalSeconds,
            durationFormatted = durationFormatted,

            DriverName = r.ReceptionEntrance?.DriverName ?? string.Empty,
            PlateNumber = r.ReceptionEntrance?.PlateNumber ?? string.Empty,
            Transportista = r.ReceptionEntrance?.Transportista ?? string.Empty,
            DucatNumbers = [.. r.EntranceDucats.Select(d => d.DucatNumber)]
            };
        }).ToList();

        return new GetReceptionEntrancesDto
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Stats = stats
        };
    }
}