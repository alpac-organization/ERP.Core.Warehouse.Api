using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Manager.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Handlers;

public class GetMerchandiseRegistryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetMerchandiseRegistryQuery, GetMerchandiseRegistryDto>(unitOfWork, errorManager)
{
    public override async Task<GetMerchandiseRegistryDto> Handle(GetMerchandiseRegistryQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region Paso de recepcion (llegada del vehiculo)
        var receptionStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);

        if(receptionStep == null)
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


        var merchandiseStep = await _unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .Skip(1)
            .FirstOrDefaultAsync(cancellationToken);
                
        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new MerchandiseRegistryListItemDto
            {
                Id = r.Id,
                PlateNumber = r.ReceptionEntrance!.PlateNumber,
                DriverName = r.ReceptionEntrance!.DriverName,
                DocumentType = r.ReceptionEntrance!.DocumentType,

                ContainerNumber = r.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration
                    ? r.CustomsDeclarations!.Details!.ContainerNumber 
                    : (r.DucatRegistry != null ? r.DucatRegistry.ContainerNumber : null),

                ArrivalDate = r.ExecutionLogs
                    .Where(l => l.WorkflowStepDefinitionCode == receptionStep.Code)
                    .Select(l => l.StartDate).First(),
                ArrivalTime = r.ExecutionLogs
                    .Where(l => l.WorkflowStepDefinitionCode == receptionStep.Code)
                    .Select(l => l.StartTime).First(),
                
                TotalDocuments = r.ReceptionEntrance!.DocumentType == DocumentType.DUCA
                    ? r.EntranceDucats.Count(d => d.DeletedAt == null)
                    : (r.CustomsDeclarations != null ? 1 : 0),
                
                CompletedDocuments = r.ReceptionEntrance!.DocumentType == DocumentType.DUCA
                    ? r.EntranceDucats.Count(d => d.DeletedAt == null && d.Status == DucaStatus.Completed)
                    : (r.CustomsDeclarations != null && r.CustomsDeclarations.Details != null ? 1 : 0)
            })
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