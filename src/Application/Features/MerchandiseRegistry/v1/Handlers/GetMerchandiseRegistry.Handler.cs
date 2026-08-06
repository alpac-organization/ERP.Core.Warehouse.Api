using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Handlers;

public class GetMerchandiseRegistryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetMerchandiseRegistryQuery, GetMerchandiseRegistryDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;
    public override async Task<GetMerchandiseRegistryDto> Handle(GetMerchandiseRegistryQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region Paso de recepcion (llegada del vehiculo)
        var receptionStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if (receptionStep == null)
        {
            return _errorManager.ThrowInternalError<GetMerchandiseRegistryDto>(
                "No se encontró la configuración del paso de recepción. Contacte al administrador.",
                "ERP:WORKFLOW_NO_CONFIGURED");
        }

        var receptionStepCode = receptionStep.Code;
        #endregion

        #region filtro base
        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ReceptionEntrance != null && r.ReceptionEntrance.DeletedAt == null);

        bool hasExplicitDate = request.StarDate.HasValue || request.EndDate.HasValue;

        if (hasExplicitDate)
        {
            var rangeStart = request.StarDate.HasValue
                ? DateOnly.FromDateTime(request.StarDate.Value)
                : DateOnly.FromDateTime(request.EndDate!.Value);

            var rangeEnd = request.EndDate.HasValue
                ? DateOnly.FromDateTime(request.EndDate.Value)
                : rangeStart;

            query = query.Where(r => r.ExecutionLogs.Any(l =>
                l.WorkflowStepDefinitionCode == receptionStepCode &&
                l.StartDate >= rangeStart &&
                l.StartDate <= rangeEnd));
        } // else: sin fecha explicita no se filtra, se muestran todos los registros

        if (!string.IsNullOrWhiteSpace(request.DriverName))
        {
            var driverFilter = request.DriverName.Trim().ToLower().Replace(" ", "");
            query = query.Where(r => r.ReceptionEntrance!.DriverName.ToLower().Replace(" ", "").Contains(driverFilter));
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
        
        if(request.DucatId.HasValue)
            query = query.Where(r => r.EntranceDucats.Any(d => d.Id == request.DucatId.Value));

        if (!string.IsNullOrWhiteSpace(request.ServiceOrderCode))
        {
            var osFilter = request.ServiceOrderCode.Trim().ToLower().Replace(" ", "");
            query = query.Where(r =>
                r.EntranceDucats.Any(d => d.ServiceOrderCode != null &&
                    d.ServiceOrderCode.ToLower().Replace(" ", "").Contains(osFilter)) ||
                (r.CustomsDeclarations != null && r.CustomsDeclarations.ServiceOrderCode != null &&
                    r.CustomsDeclarations.ServiceOrderCode.ToLower().Replace(" ", "").Contains(osFilter)));
        }
        #endregion

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<MerchandiseRegistryListItemDto>(_mapper.ConfigurationProvider, new { receptionStepCode = receptionStep.Code })
            .ToListAsync(cancellationToken);

        return new GetMerchandiseRegistryDto
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}

#region Get By Details
public class GetMerchandiseRegistryDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetMerchandiseRegistryDetailsQuery, GetMerchandiseRegistryDetailDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;
    public override async Task<GetMerchandiseRegistryDetailDto> Handle(GetMerchandiseRegistryDetailsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Include(r => r.ReceptionEntrance!)
                .ThenInclude(re => re.TransportUnit)
            .Include(r => r.DucatRegistry!)
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(cd => cd.Details)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null
            || recordEntrance.ReceptionEntrance == null
            || recordEntrance.ReceptionEntrance.DeletedAt != null)
        {
            return _errorManager.ThrowBadRequest<GetMerchandiseRegistryDetailDto>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        recordEntrance.EntranceDucats = await _unitOfWork.EntranceDucats.Entities
            .AsNoTracking()
            .Where(l => l.RecordEntranceId == recordEntrance.Id && l.DeletedAt == null)
            .Include(d => d.RegistryDetail!)
                .ThenInclude(rd => rd.Product)
            .ToListAsync(cancellationToken);

        recordEntrance.ExecutionLogs = await _unitOfWork.StepExecutionLogs.Entities
            .AsNoTracking()
            .Where(l => l.RecordEntranceId == recordEntrance.Id)
            .ToListAsync(cancellationToken);

        return _mapper.Map<GetMerchandiseRegistryDetailDto>(recordEntrance);
    }
}

#endregion