using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using ERP.Core.Manager.Api.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Handlers;

public class CreateDucatRegistryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<CreateDucatRegistryCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateDucatRegistryCommand request, CancellationToken cancellationToken)
    {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance!)
            .Include(r => r.DucatRegistry!)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if(recordEntrance == null || recordEntrance.ReceptionEntrance == null)
        {
             return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        if(recordEntrance.ReceptionEntrance.DocumentType != DocumentType.DUCA)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El detalle general solo aplica para recepciones de tipo DUCA.",
                "ERP:INVALID_DOCUMENT_TYPE");
        }

        if (recordEntrance.DucatRegistry != null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El detalle general de esta recepción ya fue registrado. Use la edición para modificarlo.",
                "ERP:DUCAT_REGISTRY_ALREADY_EXISTS");
        }

        var sanitizedContainerNumber = SanitizeAlphanumeric(request.ContainerNumber);

        if(string.IsNullOrEmpty(sanitizedContainerNumber))
        {
            return _errorManager.ThrowBadRequest<bool>(
                "El número de contenedor debe contener al menos un caracter alfanumérico.",
                "ERP:INVALID_CONTAINER_NUMBER");
        }
        
        var user = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        
        if (user == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
                "No se pudo identificar al usuario autenticado en el sistema.",
                "ERP:USER_NOT_FOUND");
        }
        var registeredByUserName = user.Fullname ?? user.UserName ?? request.UserId.ToString();


        var nowNica = NicaraguaClock.Now;
        var today = DateOnly.FromDateTime(nowNica);
        var now = TimeOnly.FromDateTime(nowNica);

        var ducatRegistry = mapper.Map<DucatRegistry>(request);
        ducatRegistry.ContainerNumber = sanitizedContainerNumber;
        ducatRegistry.RegisteredByUserId = request.UserId.ToString();
        ducatRegistry.RegisteredByUserName = registeredByUserName;
        ducatRegistry.RegisteredStartDate = request.RegisteredStartDate;
        ducatRegistry.RegisteredStartTime = request.RegisteredStartTime;
        ducatRegistry.RegisteredEndDate = today;
        ducatRegistry.RegisteredEndTime = now;

        var executionLog = await _unitOfWork.StepExecutionLogs.Entities
            .FirstOrDefaultAsync(l => l.RecordEntranceId == recordEntrance.Id && l.WorkflowStepDefinitionCode == MerchandiseRegistrationSteps.Duca, cancellationToken);

        if(executionLog == null)
        {
            executionLog = new StepExecutionLogs
            {
                Id = Guid.NewGuid(),
                RecordEntranceId = recordEntrance.Id,
                WorkflowStepDefinitionCode = MerchandiseRegistrationSteps.Duca,
                StartDate = today,
                StartTime = now,
                EndDate = null,
                EndTime = null,
                ProcessedByUserId = request.UserId.ToString(),
                ProcessedByUserName = registeredByUserName
            };
            await _unitOfWork.StepExecutionLogs.InsertExecutionLog(executionLog);
        }

        recordEntrance.CurrentStepCode = MerchandiseRegistrationSteps.Duca;

        await _unitOfWork.DucatRegistries.RegisterDucatRegistry(ducatRegistry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
     private static string SanitizeAlphanumeric(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value, "[^a-zA-Z0-9]", "").ToUpperInvariant();
    }
}