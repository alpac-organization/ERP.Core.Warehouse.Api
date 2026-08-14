using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public class GetPendingAssignmentsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetPendingAssignmentsQuery, PagedWarehouseAssignmentsDto<PendingDocumentItemDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedWarehouseAssignmentsDto<PendingDocumentItemDto>> Handle(
        GetPendingAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var receptionStepCode = await GetReceptionStepCodeAsync(_unitOfWork, cancellationToken);

        var assignedDucatIds = _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .Where(a => a.DeletedAt == null && a.EntranceDucatId != null)
            .Select(a => a.EntranceDucatId!.Value);

        var assignedCustomsIds = _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .Where(a => a.DeletedAt == null && a.CustomsDeclarationId != null)
            .Select(a => a.CustomsDeclarationId!.Value);

        var ducatsQuery = _unitOfWork.EntranceDucats.Entities
            .AsNoTracking()
            .Include(d => d.RecordEntrance!)
                .ThenInclude(r => r.ReceptionEntrance)
            .Include(d => d.RecordEntrance!)
                .ThenInclude(r => r.DucatRegistry)
            .Include(d => d.RecordEntrance!)
                .ThenInclude(r => r.ExecutionLogs)
            .Include(d => d.RegistryDetail)
            .Where(d => d.DeletedAt == null
                && d.Status == DucaStatus.Completed
                && d.RecordEntrance.DeletedAt == null
                && d.RecordEntrance.ReceptionEntrance != null
                && d.RecordEntrance.ReceptionEntrance.DeletedAt == null);

        var customsQuery = _unitOfWork.CustomsDeclarations.Entities
            .AsNoTracking()
            .Include(c => c.RecordEntrance!)
                .ThenInclude(r => r.ReceptionEntrance)
            .Include(c => c.RecordEntrance!)
                .ThenInclude(r => r.ExecutionLogs)
            .Include(c => c.Details)
            .Where(c => c.DeletedAt == null
                && c.Status == DucaStatus.Completed
                && c.RecordEntrance.DeletedAt == null
                && c.RecordEntrance.ReceptionEntrance != null
                && c.RecordEntrance.ReceptionEntrance.DeletedAt == null);

        if (request.DocumentType.HasValue)
        {
            if (request.DocumentType.Value == DocumentType.DUCA)
                customsQuery = customsQuery.Where(_ => false);
            else if (request.DocumentType.Value == DocumentType.CustomsDeclaration)
                ducatsQuery = ducatsQuery.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(request.DriverName))
        {
            var driverFilter = request.DriverName.Trim().ToLower().Replace(" ", "");
            ducatsQuery = ducatsQuery.Where(d => d.RecordEntrance.ReceptionEntrance!.DriverName.ToLower().Replace(" ", "").Contains(driverFilter));
            customsQuery = customsQuery.Where(c => c.RecordEntrance.ReceptionEntrance!.DriverName.ToLower().Replace(" ", "").Contains(driverFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
        {
            var plateFilter = request.PlateNumber.Trim().ToLower().Replace(" ", "");
            ducatsQuery = ducatsQuery.Where(d => d.RecordEntrance.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter));
            customsQuery = customsQuery.Where(c => c.RecordEntrance.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            var docFilter = request.DocumentNumber.Trim().ToLower().Replace(" ", "");
            ducatsQuery = ducatsQuery.Where(d => d.DucatNumber.ToLower().Replace(" ", "").Contains(docFilter));
            customsQuery = customsQuery.Where(c => c.CustomsDeclarationNumber.ToLower().Replace(" ", "").Contains(docFilter));
        }

        var ducats = await ducatsQuery
            .Where(d => !assignedDucatIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        var customs = await customsQuery
            .Where(c => !assignedCustomsIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var items = new List<PendingDocumentItemDto>();

        foreach (var ducat in ducats)
        {
            var reception = ducat.RecordEntrance;
            items.Add(new PendingDocumentItemDto
            {
                Id = ducat.Id,
                ReceptionId = reception.Id,
                DocumentType = DocumentType.DUCA,
                DocumentNumber = ducat.DucatNumber,
                ServiceOrderCode = ducat.ServiceOrderCode,
                MerchandiseName = ducat.RegistryDetail?.MerchandiseName,
                TotalBultos = ducat.RegistryDetail?.TotalBultos,
                TotalWeight = ducat.RegistryDetail?.TotalWeight,
                PlateNumber = reception.ReceptionEntrance!.PlateNumber,
                DriverName = reception.ReceptionEntrance.DriverName,
                ContainerNumber = reception.DucatRegistry?.ContainerNumber,
                ArrivalDate = reception.ExecutionLogs
                    .FirstOrDefault(l => l.WorkflowStepDefinitionCode == receptionStepCode)?.StartDate,
                ArrivalTime = reception.ExecutionLogs
                    .FirstOrDefault(l => l.WorkflowStepDefinitionCode == receptionStepCode)?.StartTime
            });
        }

        foreach (var declaration in customs)
        {
            var reception = declaration.RecordEntrance;
            items.Add(new PendingDocumentItemDto
            {
                Id = declaration.Id,
                ReceptionId = reception.Id,
                DocumentType = DocumentType.CustomsDeclaration,
                DocumentNumber = declaration.CustomsDeclarationNumber,
                ServiceOrderCode = declaration.ServiceOrderCode,
                MerchandiseName = declaration.Details?.Product,
                TotalBultos = declaration.Details?.Packages,
                TotalWeight = null,
                PlateNumber = reception.ReceptionEntrance!.PlateNumber,
                DriverName = reception.ReceptionEntrance.DriverName,
                ContainerNumber = declaration.Details?.ContainerNumber,
                ArrivalDate = reception.ExecutionLogs
                    .FirstOrDefault(l => l.WorkflowStepDefinitionCode == receptionStepCode)?.StartDate,
                ArrivalTime = reception.ExecutionLogs
                    .FirstOrDefault(l => l.WorkflowStepDefinitionCode == receptionStepCode)?.StartTime
            });
        }

        var ordered = items
            .OrderByDescending(i => i.ArrivalDate ?? DateOnly.MinValue)
            .ThenByDescending(i => i.ArrivalTime ?? TimeOnly.MinValue)
            .ToList();

        var totalCount = ordered.Count;
        var data = ordered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedWarehouseAssignmentsDto<PendingDocumentItemDto>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    internal static async Task<string> GetReceptionStepCodeAsync(
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var receptionStep = await unitOfWork.WorkflowStepDefinitions.Entities
            .OrderBy(x => x.ExecutionOrder)
            .FirstOrDefaultAsync(cancellationToken);

        return receptionStep?.Code ?? WorkflowStepCodes.Reception;
    }
}

public class GetWarehouseAssignmentsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetWarehouseAssignmentsQuery, PagedWarehouseAssignmentsDto<WarehouseAssignmentListItemDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedWarehouseAssignmentsDto<WarehouseAssignmentListItemDto>> Handle(
        GetWarehouseAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var query = _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .Include(a => a.Warehouse)
            .Include(a => a.Section)
            .Include(a => a.Rack)
            .Include(a => a.Lot)
            .Include(a => a.EntranceDucat!)
                .ThenInclude(d => d.RecordEntrance!)
                    .ThenInclude(r => r.ReceptionEntrance)
            .Include(a => a.CustomsDeclaration!)
                .ThenInclude(c => c.RecordEntrance!)
                    .ThenInclude(r => r.ReceptionEntrance)
            .Include(a => a.UnloadingDetails!)
                .ThenInclude(u => u.CrewAssignments)
            .Include(a => a.UnloadingDetails!)
                .ThenInclude(u => u.MachineryAssignments)
            .Where(a => a.DeletedAt == null
                && (a.EntranceDucatId != null || a.CustomsDeclarationId != null));

        if (!string.IsNullOrWhiteSpace(request.DriverName))
        {
            var driverFilter = request.DriverName.Trim().ToLower().Replace(" ", "");
            query = query.Where(a =>
                (a.EntranceDucat != null && a.EntranceDucat.RecordEntrance.ReceptionEntrance!.DriverName.ToLower().Replace(" ", "").Contains(driverFilter)) ||
                (a.CustomsDeclaration != null && a.CustomsDeclaration.RecordEntrance.ReceptionEntrance!.DriverName.ToLower().Replace(" ", "").Contains(driverFilter)));
        }

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
        {
            var plateFilter = request.PlateNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(a =>
                (a.EntranceDucat != null && a.EntranceDucat.RecordEntrance.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter)) ||
                (a.CustomsDeclaration != null && a.CustomsDeclaration.RecordEntrance.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(a => a.AssignedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var data = entities.Select(a =>
        {
            var isDuca = a.EntranceDucat != null;
            var document = isDuca ? (object)a.EntranceDucat! : (object)a.CustomsDeclaration!;
            var reception = isDuca
                ? a.EntranceDucat!.RecordEntrance
                : a.CustomsDeclaration!.RecordEntrance;

            return new WarehouseAssignmentListItemDto
            {
                Id = a.Id,
                ReceptionId = reception.Id,
                DocumentId = isDuca ? a.EntranceDucatId!.Value : a.CustomsDeclarationId!.Value,
                DocumentType = isDuca ? DocumentType.DUCA : DocumentType.CustomsDeclaration,
                DocumentNumber = isDuca
                    ? a.EntranceDucat!.DucatNumber
                    : a.CustomsDeclaration!.CustomsDeclarationNumber,
                PlateNumber = reception.ReceptionEntrance!.PlateNumber,
                DriverName = reception.ReceptionEntrance.DriverName,
                WarehouseName = a.Warehouse.WarehouseName,
                WarehouseType = a.Warehouse.WarehouseType,
                SectionCode = a.Section?.Code,
                RackCode = a.Rack?.Code,
                LotCode = a.Lot?.Code,
                AssignedAt = a.AssignedAt,
                IsCompleted = a.UnloadingDetails != null && a.UnloadingDetails.UnloadingEndTime != null,
                CrewCount = a.UnloadingDetails?.CrewAssignments.Sum(c => c.PersonaCount) ?? 0,
                MachineryCount = a.UnloadingDetails?.MachineryAssignments.Count ?? 0
            };
        }).ToList();

        return new PagedWarehouseAssignmentsDto<WarehouseAssignmentListItemDto>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
