using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Manager.Api.Domain.Enums;
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
        
        #region 2. Filtro Base
        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate == targetDate) &&
                r.ReceptionEntrance != null &&
                r.ReceptionEntrance.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.DriverName))
        {
            var driverFilter = request.DriverName.Trim().ToLower();
            query = query.Where(r => r.ReceptionEntrance!.DriverName.ToLower().Contains(driverFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
        {
            var plateFilter = request.PlateNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(r => r.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter));
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
        #endregion
        

        #region 3. Stats del dia (Filtro de busqueda)
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
            .Where(r => r.ReceptionEntrance == null || 
                    r.ReceptionEntrance.TransportUnitExitDate == null)
            .CountAsync(cancellationToken);

        var totalExits = await recepcionadosQuery
            .Where(r => r.ReceptionEntrance != null &&
                        r.ReceptionEntrance.TransportUnitExitDate != null)
            .CountAsync(cancellationToken);
        #endregion

        #region 3. Conteo

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReceptionEntranceListItemDto
            {
                Id = r.Id,
                Status = r.Status,
                PlateNumber = r.ReceptionEntrance!.PlateNumber,
                DriverName = r.ReceptionEntrance!.DriverName,
                DocumentType = r.ReceptionEntrance!.DocumentType,
                ArrivalTime = r.ExecutionLogs
                                .Where(l => l.WorkflowStepDefinitionCode == receptionStepCode)
                                .Select(l => l.StartTime).First(),
            })
            .ToListAsync(cancellationToken);
        #endregion

        return new GetReceptionEntrancesDto
        {
            Data        = data,
            TotalCount  = totalCount,
            PageNumber  = request.PageNumber,
            PageSize    = request.PageSize,
            Stats       = new ReceptionEntranceStatsDto
            {
                    TotalEntries       = totalEntries,
                    TotalOnSite       = totalOnSite,
                    TotalExists    = totalExits

            }
        };
    }
}

#region Obtener con Detaller
public class GetReceptionEntranceDetailHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager)
    : IRequestHandler<GetReceptionEntranceDetailQuery, ReceptionEntranceDetailDto>
{
    public async Task<ReceptionEntranceDetailDto> 
        Handle(GetReceptionEntranceDetailQuery request, CancellationToken cancellationToken)
    {
        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Include(r => r.ReceptionEntrance!)
                .ThenInclude(re => re.TransportUnit)
            .Include(r => r.EntranceDucats.Where(d => d.DeletedAt == null))
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(cd => cd.Details)
            .Include(r => r.ExecutionLogs)
            .FirstOrDefaultAsync(r => r.Id == request.RecordId && r.DeletedAt == null, cancellationToken);
        
        if (recordEntrance == null
            || recordEntrance.ReceptionEntrance == null
            || recordEntrance.ReceptionEntrance.DeletedAt != null)
        {
            return _errorManager.ThrowBadRequest<ReceptionEntranceDetailDto>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        var reception = recordEntrance.ReceptionEntrance;

        #region Buscar el step de recepcion para calcular duracion
        var receptionStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);
        
        if(receptionStep == null)
        {
            return _errorManager.ThrowInternalError<ReceptionEntranceDetailDto>(
            "No se encontró una configuración para el flujo de trabajo (WorkflowStepDefinition). Contacte al administrador.",
            "ERP:WORKFLOW_NOT_CONFIGURED");
        }

        var receptionLog = recordEntrance.ExecutionLogs
            .FirstOrDefault(l => l.WorkflowStepDefinitionCode == receptionStep.Code);
        
        ExecutionLogDetailDto? executionLogDto = null;

        if(receptionLog != null)
        {
            int? durationSeconds = null;
            string? durationFormatted = null;

            if(receptionLog.EndDate.HasValue && receptionLog.EndTime.HasValue)
            {
                var start    = receptionLog.StartDate.ToDateTime(receptionLog.StartTime);
                var end      = receptionLog.EndDate.Value.ToDateTime(receptionLog.EndTime.Value);
                var duration = end - start;

                durationSeconds   = (int)duration.TotalSeconds;
                durationFormatted = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }

            executionLogDto = new ExecutionLogDetailDto
            {
                StartDate            = receptionLog.StartDate,
                StartTime            = receptionLog.StartTime,
                EndDate              = receptionLog.EndDate,
                EndTime              = receptionLog.EndTime,
                ProcessedByUserName  = receptionLog.ProcessedByUserName,
                DurationTotalSeconds = durationSeconds,
                DurationFormatted    = durationFormatted
            };
        }
        #endregion

        return new ReceptionEntranceDetailDto
        {
            Id = recordEntrance.Id,
            Status                = recordEntrance.Status,
            IsConsolidated        = recordEntrance.IsConsolidated,

            CountryOfOrigin       = reception.CountryOfOrigin,
            Aduana                = reception.Aduana,
            PlateNumber           = reception.PlateNumber,
            TrailerChassis        = reception.TrailerChassis,
            DriverLicense         = reception.DriverLicense,
            Transportista         = reception.Transportista,
            TransportUnitId       = reception.TransportUnitId,
            TransportUnitName     = reception.TransportUnit?.Name,
            DriverName            = reception.DriverName,
            SealNumber            = reception.SealNumber,
            DocumentType          = reception.DocumentType,
            TransportUnitExitDate = reception.TransportUnitExitDate,
            TransportUnitExitTime = reception.TransportUnitExitTime,
            UpdatedByUserName     = reception.UpdatedByUserName,
            UpdatedDate           = reception.UpdatedDate,
            UpdatedTime           = reception.UpdatedTime,

            Ducats = reception.DocumentType == DocumentType.DUCA
                ? [.. recordEntrance.EntranceDucats
                    .Where(d => d.DeletedAt == null)
                    .Select(d => new EntranceDucatDetailItemDto
                    {
                        Id = d.Id,
                        DucatNumber = d.DucatNumber,
                    })] : null,
            
            CustomsDeclaration = reception.DocumentType == DocumentType.CustomsDeclaration
                                && recordEntrance.CustomsDeclarations != null
                ? new CustomsDeclarationDetailDto
                {
                    CustomsDecarationNumber = recordEntrance.CustomsDeclarations.CustomsDeclarationNumber,
                    Packages = recordEntrance.CustomsDeclarations.Details?.Packages,
                    Customer = recordEntrance.CustomsDeclarations.Details?.Customer,
                    Product = recordEntrance.CustomsDeclarations.Details?.Product,
                    ContainerNumber = recordEntrance.CustomsDeclarations.Details?.ContainerNumber
                } : null,

            ExecutionLog = executionLogDto
        };
    }
}
#endregion

#region Obtener vehiculos
public class GetTransportUnitsHandlers(IUnitOfWork _unitOfWork) : IRequestHandler<GetTreansportUnitsQuery, List<TransportUnitListItemDto>>
{
    public async Task<List<TransportUnitListItemDto>> Handle(GetTreansportUnitsQuery request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.TransportUnit.Entities
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .Select(t => new TransportUnitListItemDto
            {
                Id = t.Id,
                Name = t.Name
            })
            .ToListAsync(cancellationToken);
    }
}
#endregion