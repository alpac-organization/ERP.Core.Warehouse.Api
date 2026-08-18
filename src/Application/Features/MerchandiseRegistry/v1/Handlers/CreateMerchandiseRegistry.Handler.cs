using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using ERP.Core.Database.Domain.Enums;
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

        if (recordEntrance == null || recordEntrance.ReceptionEntrance == null)
        {
            return _errorManager.ThrowBadRequest<bool>(
               "El registro de recepción no fue encontrado o ya ha sido eliminado.",
               "ERP:RECEPTION_NOT_FOUND");
        }

        if (recordEntrance.ReceptionEntrance.DocumentType != DocumentType.DUCA)
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

        if (string.IsNullOrEmpty(sanitizedContainerNumber))
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
        // ducatRegistry.ContainerNumber = sanitizedContainerNumber;
        ducatRegistry.RegisteredByUserId = request.UserId.ToString();
        ducatRegistry.RegisteredByUserName = registeredByUserName;
        ducatRegistry.RegisteredStartDate = request.RegisteredStartDate;
        ducatRegistry.RegisteredStartTime = request.RegisteredStartTime;
        ducatRegistry.RegisteredEndDate = today;
        ducatRegistry.RegisteredEndTime = now;
        ducatRegistry.Status = DucaStatus.Pending;

        var executionLog = await _unitOfWork.StepExecutionLogs.Entities
            .FirstOrDefaultAsync(l => l.RecordEntranceId == recordEntrance.Id && l.WorkflowStepDefinitionCode == MerchandiseRegistrationSteps.Duca, cancellationToken);

        if (executionLog == null)
        {
            executionLog = new StepExecutionLogs
            {
                Id = Guid.NewGuid(),
                RecordEntranceId = recordEntrance.Id,
                WorkflowStepDefinitionCode = MerchandiseRegistrationSteps.Duca,
                StartDate = request.RegisteredStartDate ?? today,
                StartTime = request.RegisteredStartTime ?? now,
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


public class CreateDucatRegistryDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<CreateDucatRegistryDetailCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(CreateDucatRegistryDetailCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        #region 1. Busqueda y validaciones de existencias
        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance!)
            .Include(r => r.DucatRegistry!)
            .Include(r => r.EntranceDucats)
                .ThenInclude(d => d.RegistryDetail)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null || recordEntrance.ReceptionEntrance == null)
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");

        if (recordEntrance.ReceptionEntrance.DocumentType != DocumentType.DUCA)
            return _errorManager.ThrowBadRequest<bool>(
                "El detalle por DUCA solo aplica para recepciones de tipo DUCA.",
                "ERP:INVALID_DOCUMENT_TYPE");

        if (recordEntrance.DucatRegistry == null)
            return _errorManager.ThrowBadRequest<bool>(
                "Debe registrar primero el detalle general (Dato General) de esta recepción.",
                "ERP:DUCAT_REGISTRY_NOT_FOUND");

        var entranceDucat = recordEntrance.EntranceDucats
            .FirstOrDefault(d => d.Id == request.EntranceDucatId && d.DeletedAt == null);

        if (entranceDucat == null)
            return _errorManager.ThrowBadRequest<bool>(
                "El DUCA indicado no existe o no pertenece a esta recepción.",
                "ERP:DUCAT_NOT_FOUND");

        if (entranceDucat.RegistryDetail != null)
            return _errorManager.ThrowBadRequest<bool>(
                "Este DUCA ya tiene un detalle registrado. Use la edición para modificarlo.",
                "ERP:DUCAT_DETAIL_ALREADY_EXISTS");
        #endregion

        #region 2. Validacion de Mercaderia
        var merchandise = await _unitOfWork.Merchandises.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.MerchandiseId && p.DeletedAt == null, cancellationToken);

        if (merchandise == null)
            return _errorManager.ThrowBadRequest<bool>(
                "La mercadería indicada no existe en el sistema.",
                "ERP:MERCHANDISE_NOT_FOUND");
        #endregion

        #region 2.b Validacion de orden de servicio
        if (entranceDucat.ServiceOrderId != null)
            return _errorManager.ThrowBadRequest<bool>(
                "Este DUCA ya tiene una orden de servicio asignada.",
            "ERP:DUCAT_SERVICE_ORDER_ALREADY_ASSIGNED");

        var serviceOrder = await _unitOfWork.ServiceOrders.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(so => so.Id == request.ServiceOrderId && so.DeletedAt == null, cancellationToken);

        if (serviceOrder == null)
            return _errorManager.ThrowBadRequest<bool>(
                "La orden de servicio indicada no existe.",
            "ERP:SERVICE_ORDER_NOT_FOUND");

        // ¿Otro documento usa esta misma OS?
        var serviceOrderAlreadyUsed = await _unitOfWork.EntranceDucats.Entities
            .AnyAsync(d => d.ServiceOrderId == request.ServiceOrderId && d.DeletedAt == null, cancellationToken)
            || await _unitOfWork.CustomsDeclarations.Entities
            .AnyAsync(c => c.ServiceOrderId == request.ServiceOrderId && c.DeletedAt == null, cancellationToken);

        if (serviceOrderAlreadyUsed)
            return _errorManager.ThrowBadRequest<bool>(
            "La orden de servicio indicada ya está asignada a otro documento.",
            "ERP:SERVICE_ORDER_ALREADY_IN_USE");

        entranceDucat.ServiceOrderId = serviceOrder.Id;
        entranceDucat.ServiceOrderCode = serviceOrder.Code;
        #endregion

        #region 3. Usuario actual
        var user = await _unitOfWork.Users.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return _errorManager.ThrowBadRequest<bool>(
                "No se pudo identificar al usuario autenticado en el sistema.",
                "ERP:USER_NOT_FOUND");

        var currentUserName = user.Fullname ?? user.UserName ?? request.UserId.ToString();
        #endregion

        #region 4. Registro del detalle
        var nowNica = NicaraguaClock.Now;
        var today = DateOnly.FromDateTime(nowNica);
        var now = TimeOnly.FromDateTime(nowNica);

        var registryDetail = mapper.Map<DucatRegistryDetails>(request);
        registryDetail.EntranceDucatId = entranceDucat.Id;
        registryDetail.MerchandiseName = merchandise.MerchandiseName;
        registryDetail.RegisteredByUserId = request.UserId.ToString();
        registryDetail.RegisteredByUserName = currentUserName;
        registryDetail.RegisteredStartDate = request.RegisteredStartDate;
        registryDetail.RegisteredStartTime = request.RegisteredStartTime;
        registryDetail.RegisteredEndDate = today;
        registryDetail.RegisteredEndTime = now;

        await _unitOfWork.DucatRegistryDetails.RegisterDucatRegistryDetails(registryDetail);
        #endregion

        #region 5. Completar el DUCA
        entranceDucat.Status = DucaStatus.Completed;
        #endregion

        #region 6. Cerrar el StepExecutionLog Solo si todos los DUCA esta 'Completed'
        var allDucatsCompleted = recordEntrance.EntranceDucats
            .Where(d => d.DeletedAt == null)
            .All(d => d.Status == DucaStatus.Completed);

        if (allDucatsCompleted)
        {
            var executionLog = await _unitOfWork.StepExecutionLogs.Entities
                .FirstOrDefaultAsync(l =>
                    l.RecordEntranceId == recordEntrance.Id &&
                    l.WorkflowStepDefinitionCode == MerchandiseRegistrationSteps.Duca,
                    cancellationToken);

            if (executionLog != null)
            {
                executionLog.EndDate = today;
                executionLog.EndTime = now;
                executionLog.FinishedByUserId = request.UserId.ToString();
                executionLog.FinishedByUserName = currentUserName;
            }

            recordEntrance.DucatRegistry!.Status = DucaStatus.Completed;
        }
        #endregion

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

#region Asignar OS a Declaracion Aduanera
public class AssignServiceOrderToCustomsDeclarationHandlers(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<AssignServiceOrderToCustomsDeclarationCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(AssignServiceOrderToCustomsDeclarationCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .Include(r => r.ReceptionEntrance!)
            .Include(r => r.CustomsDeclarations!)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null || recordEntrance.ReceptionEntrance == null)
            return _errorManager.ThrowBadRequest<bool>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");

        if (recordEntrance.ReceptionEntrance.DocumentType != DocumentType.CustomsDeclaration)
            return _errorManager.ThrowBadRequest<bool>(
                "Esta operación solo aplica para recepciones de tipo Declaración Aduanera.",
                "ERP:INVALID_DOCUMENT_TYPE");

        if (recordEntrance.CustomsDeclarations == null)
            return _errorManager.ThrowBadRequest<bool>(
                "Esta recepción no tiene una declaración aduanera registrada.",
                "ERP:CUSTOMS_DECLARATION_NOT_FOUND");

        if (recordEntrance.CustomsDeclarations.ServiceOrderId != null)
            return _errorManager.ThrowBadRequest<bool>(
                "Esta declaración aduanera ya tiene una orden de servicio asignada.",
                "ERP:CUSTOMS_DECLARATION_SERVICE_ORDER_ALREADY_ASSIGNED");

        var serviceOrder = await _unitOfWork.ServiceOrders.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(so => so.Id == request.ServiceOrderId && so.DeletedAt == null, cancellationToken);

        if (serviceOrder == null)
            return _errorManager.ThrowBadRequest<bool>(
                "La orden de servicio indicada no existe.",
                "ERP:SERVICE_ORDER_NOT_FOUND");

        var serviceOrderAlreadyUsed = await _unitOfWork.EntranceDucats.Entities
            .AnyAsync(d => d.ServiceOrderId == request.ServiceOrderId && d.DeletedAt == null, cancellationToken)
            || await _unitOfWork.CustomsDeclarations.Entities
            .AnyAsync(c => c.ServiceOrderId == request.ServiceOrderId && c.DeletedAt == null, cancellationToken);

        if (serviceOrderAlreadyUsed)
            return _errorManager.ThrowBadRequest<bool>(
                "La orden de servicio indicada ya está asignada a otro documento.",
                "ERP:SERVICE_ORDER_ALREADY_IN_USE");

        recordEntrance.CustomsDeclarations.ServiceOrderId = serviceOrder.Id;
        recordEntrance.CustomsDeclarations.ServiceOrderCode = serviceOrder.Code;
        recordEntrance.CustomsDeclarations.Status = DucaStatus.Completed;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;

    }
}
#endregion