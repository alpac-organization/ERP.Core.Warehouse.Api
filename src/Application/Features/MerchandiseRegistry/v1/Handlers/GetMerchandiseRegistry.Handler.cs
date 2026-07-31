using System.Reflection.Metadata;
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

#region Get By Details
public class GetMerchandiseRegistryDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetMerchandiseRegistryDetailsQuery, GetMerchandiseRegistryDetailDto>(unitOfWork, errorManager)
{
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

        var reception = recordEntrance.ReceptionEntrance;
        var registrationStepCode = reception.DocumentType == DocumentType.CustomsDeclaration
            ? "RECEP" : "REME";
        var merchandiseLog = recordEntrance.ExecutionLogs
            .FirstOrDefault(l => l.WorkflowStepDefinitionCode == registrationStepCode);
        
        MerchandiseDucaRegistryDetailDto? ducaRegistry = null;
        MerchandiseCustomsDeclarationDetailDto? customsDeclaration = null;

        if (reception.DocumentType == DocumentType.DUCA)
        {
            var registryDetailsByDucat = recordEntrance.DucatRegistry?.Details
                .ToDictionary(d => d.EntranceDucatId) ?? [];

            var ducats = recordEntrance.EntranceDucats.Select(d =>
            {
                registryDetailsByDucat.TryGetValue(d.Id, out var detail);

                return new MerchandiseDucatDetailDto
                {
                    Id = d.Id,
                    DucatNumber = d.DucatNumber,
                    Status = d.Status,

                    ProductId = detail?.ProductId,
                    ProductName = detail?.Product?.ProductName,
                    TotalBultos = detail?.TotalBultos,
                    TotalWeight = detail?.TotalWeight,
                    ProductDescription = detail?.ProductDescription,
                    Remitente = detail?.Remitente,
                    DestinationAreaObservation = detail?.DestinationAreaObservation,
                    UpdatedByUserName = detail?.UpdatedByUserName,
                    UpdatedDate = detail?.UpdatedDate,
                    UpdatedTime = detail?.UpdatedTime
                };
            }).ToList();

            ducaRegistry = new MerchandiseDucaRegistryDetailDto
            {
                Empresa = recordEntrance.DucatRegistry?.Empresa,
                GeneralObservations = recordEntrance.DucatRegistry?.GeneralObservations,
                IsInTransit = recordEntrance.DucatRegistry?.IsInTransit,
                UpdatedByUserName = recordEntrance.DucatRegistry?.UpdatedByUserName,
                UpdatedDate = recordEntrance.DucatRegistry?.UpdatedDate,
                UpdatedTime = recordEntrance.DucatRegistry?.UpdatedTime,
                Ducats = ducats
            };
        }
        else if (reception.DocumentType == DocumentType.CustomsDeclaration && recordEntrance.CustomsDeclarations != null)
        {
            customsDeclaration = new MerchandiseCustomsDeclarationDetailDto
            {
                CustomsDeclarationNumber = recordEntrance.CustomsDeclarations.CustomsDeclarationNumber,
                Packages = recordEntrance.CustomsDeclarations.Details?.Packages,
                Customer = recordEntrance.CustomsDeclarations.Details?.Customer,
                Product = recordEntrance.CustomsDeclarations.Details?.Product,
            };
        }

        var containerNumber = reception.DocumentType == DocumentType.CustomsDeclaration
            ? recordEntrance.CustomsDeclarations?.Details?.ContainerNumber
            : recordEntrance.DucatRegistry?.ContainerNumber;

        return new GetMerchandiseRegistryDetailDto
        {
            Id = recordEntrance.Id,
            Status = recordEntrance.Status,

            Reception = new MerchandiseReceptionDetailDto
            {
                CountryOfOrigin = reception.CountryOfOrigin,
                Aduana = reception.Aduana,
                PlateNumber = reception.PlateNumber,
                TrailerChassis = reception.TrailerChassis,
                DriverLicense = reception.DriverLicense,
                Transportista = reception.Transportista,
                TransportUnitId = reception.TransportUnitId,
                TransportUnitName = reception.TransportUnit?.Name,
                DriverName = reception.DriverName,
                SealNumber = reception.SealNumber,
                DocumentType = reception.DocumentType,
                TransportUnitExitDate = reception.TransportUnitExitDate,
                TransportUnitExitTime = reception.TransportUnitExitTime,
                ContainerNumber = containerNumber
            },

            MerchandiseRegistration = new MerchandiseRegistrationLogDto
            {
                MerchandiseRegistrationDate = merchandiseLog?.StartDate,
                MerchandiseRegistrationTime = merchandiseLog?.StartTime,
                MerchandiseRegisteredByUserName = merchandiseLog?.ProcessedByUserName
            },

            DucaRegistry = ducaRegistry,
            CustomsDeclaration = customsDeclaration
        };
    }
}

#endregion