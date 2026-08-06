using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class GetReceptionEntrancesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetReceptionEntrancesQuery, GetReceptionEntrancesDto>(unitOfWork, errorManager)
{
    public readonly IMapper _mapper = mapper;
    public override async Task<GetReceptionEntrancesDto> Handle(GetReceptionEntrancesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 1. Busqueda de Code
        var receptionStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (receptionStep == null)
        {
            return _errorManager.ThrowInternalError<GetReceptionEntrancesDto>(
                "No se encontró una configuración para el flujo de trabajo (WorkflowStepDefinition). Contacte al administrador.",
                "ERP:WORKFLOW_NOT_CONFIGURED");
        }

        var receptionStepCode = receptionStep.Code;
        #endregion

        var statsTargetDate = request.StartDate.HasValue
            ? DateOnly.FromDateTime(request.StartDate.Value)
            : NicaraguaClock.Today;

        #region 2. Filtro Base (busqueda global)
        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode) &&
                r.ReceptionEntrance != null &&
                r.ReceptionEntrance.DeletedAt == null);
        
        bool hasSearchFilters = 
            !string.IsNullOrWhiteSpace(request.DriverName) ||
            !string.IsNullOrWhiteSpace(request.PlateNumber) ||
            request.DocumentType.HasValue ||
            !string.IsNullOrWhiteSpace(request.DocumentNumber) ||
            !string.IsNullOrWhiteSpace(request.DucatNumber) ||
            request.DucatId.HasValue;
        
        bool hasExplicitDate = request.StartDate.HasValue || request.EndDate.HasValue;

        if (hasExplicitDate)
        {
            //fecha explicita: se respeta el rango indicado sin importar otros filtros
            var rangeStart = request.StartDate.HasValue
                ? DateOnly.FromDateTime(request.StartDate.Value)
                : DateOnly.FromDateTime(request.EndDate!.Value);

            var rangeEnd = request.EndDate.HasValue
                ?DateOnly.FromDateTime(request.EndDate.Value)
                : rangeStart;
            
            query = query.Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate >= rangeStart &&
                l.StartDate <= rangeEnd));
        }
        else if (!hasSearchFilters)
        {
            var today = NicaraguaClock.Today;
            query = query.Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate == today));
        }

        // else: hay filtros de busqueda pero no fecha explicita

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

        if (request.DocumentType.HasValue)
            query = query.Where(r => r.ReceptionEntrance!.DocumentType == request.DocumentType.Value);

        if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            var docFilter = request.DocumentNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(r =>
                r.EntranceDucats.Any(d => d.DucatNumber.ToLower().Replace(" ", "").Contains(docFilter)) ||
                (r.CustomsDeclarations != null &&
                 r.CustomsDeclarations.CustomsDeclarationNumber.ToLower().Replace(" ", "").Contains(docFilter)));
        }

        if (!string.IsNullOrWhiteSpace(request.DucatNumber))
        {
            var ducatFilter = request.DucatNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(r => r.EntranceDucats.Any(d => d.DucatNumber.ToLower().Replace(" ", "").Contains(ducatFilter)));
        }

        if (request.DucatId.HasValue)
            query = query.Where(r => r.EntranceDucats.Any(d => d.Id == request.DucatId.Value));
        #endregion


        #region 3. Stats del dia (Filtro de busqueda)
        var totalEntries = await _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate == statsTargetDate))
            .CountAsync(cancellationToken);

        var recepcionadosQuery = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate <= statsTargetDate));

        var totalOnSite = await recepcionadosQuery
            .Where(r => r.ReceptionEntrance == null ||
                    r.ReceptionEntrance.TransportUnitExitDate == null ||
                    r.ReceptionEntrance.TransportUnitExitTime == null)
            .CountAsync(cancellationToken);

        var totalExits = await recepcionadosQuery
            .Where(r => r.ReceptionEntrance != null &&
                        r.ReceptionEntrance.TransportUnitExitDate != null &&
                        r.ReceptionEntrance.TransportUnitExitTime != null)
            .CountAsync(cancellationToken);
        #endregion

        #region 4. Conteo
        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<ReceptionEntranceListItemDto>(_mapper.ConfigurationProvider, new { receptionStepCode })
            .ToListAsync(cancellationToken);
        #endregion

        return new GetReceptionEntrancesDto
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            Stats = new ReceptionEntranceStatsDto
            {
                TotalEntries = totalEntries,
                TotalOnSite = totalOnSite,
                TotalExists = totalExits

            }
        };
    }
}

#region Obtener con Detaller
public class GetReceptionEntranceDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetReceptionEntranceDetailQuery, ReceptionEntranceDetailDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<ReceptionEntranceDetailDto>
        Handle(GetReceptionEntranceDetailQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

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

        #region Buscar el step de recepcion para calcular duracion
        var receptionStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (receptionStep == null)
        {
            return _errorManager.ThrowInternalError<ReceptionEntranceDetailDto>(
            "No se encontró una configuración para el flujo de trabajo (WorkflowStepDefinition). Contacte al administrador.",
            "ERP:WORKFLOW_NOT_CONFIGURED");
        }

        return _mapper.Map<ReceptionEntranceDetailDto>(recordEntrance, opts =>
        {
            opts.Items["receptionStepCode"] = receptionStep.Code;
        });
        #endregion
    }
}
#endregion

#region Obtener vehiculos
public class GetTransportUnitsHandlers(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetTreansportUnitsQuery, List<TransportUnitListItemDto>>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<List<TransportUnitListItemDto>> Handle(GetTreansportUnitsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var transportUnits = await _unitOfWork.TransportUnit.Entities
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<TransportUnitListItemDto>>(transportUnits);
    }
}
#endregion