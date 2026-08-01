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
        #endregion

        #region filtro base
        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Where(r => r.ReceptionEntrance != null && r.ReceptionEntrance.DeletedAt == null);
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
            .Include(r => r.EntranceDucats.Where(d => d.DeletedAt == null))
            .Include(r => r.DucatRegistry!)
                .ThenInclude(dr => dr.Details)
                    .ThenInclude(d => d.Product)
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(cd => cd.Details)
            .Include(r => r.ExecutionLogs)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null
            || recordEntrance.ReceptionEntrance == null
            || recordEntrance.ReceptionEntrance.DeletedAt != null)
        {
            return _errorManager.ThrowBadRequest<GetMerchandiseRegistryDetailDto>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        return _mapper.Map<GetMerchandiseRegistryDetailDto>(recordEntrance);
    }
}

#endregion