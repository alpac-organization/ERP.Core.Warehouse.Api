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

public class GetPendingAssignmentsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<GetPendingAssignmentsQuery, PagedWarehouseAssignmentsDto<PendingAssignmentItemDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedWarehouseAssignmentsDto<PendingAssignmentItemDto>> Handle(
        GetPendingAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Include(r => r.ReceptionEntrance!)
            .Include(r => r.EntranceDucats)
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(cd => cd.Details)
            .Include(r => r.DucatRegistry)
            .Include(r => r.ExecutionLogs)
            .Where(r => r.ReceptionEntrance != null
                && r.ReceptionEntrance.DeletedAt == null
                && r.DeletedAt == null
                && r.Assignment == null);

        if (request.DocumentType.HasValue)
        {
            query = ApplyStepTwoCompletedFilter(query, request.DocumentType.Value);
        }
        else
        {
            query = query.Where(r =>
                (r.ReceptionEntrance!.DocumentType == DocumentType.DUCA &&
                 r.EntranceDucats.Any(d => d.DeletedAt == null) &&
                 r.EntranceDucats.Where(d => d.DeletedAt == null).All(d => d.Status == DucaStatus.Completed)) ||
                (r.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration &&
                 r.CustomsDeclarations != null &&
                 r.CustomsDeclarations.Details != null));
        }

        if (!string.IsNullOrWhiteSpace(request.DriverName))
        {
            var driverFilter = request.DriverName.Trim().ToLower().Replace(" ", "");
            query = query.Where(r => r.ReceptionEntrance!.DriverName.ToLower().Replace(" ", "").Contains(driverFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
        {
            var plateFilter = request.PlateNumber.Trim().ToLower().Replace(" ", "");
            // query = query.Where(r => r.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            var docFilter = request.DocumentNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(r =>
                r.EntranceDucats.Any(d => d.DucatNumber.ToLower().Replace(" ", "").Contains(docFilter)) ||
                (r.CustomsDeclarations != null &&
                 r.CustomsDeclarations.CustomsDeclarationNumber.ToLower().Replace(" ", "").Contains(docFilter)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var receptionStepCode = await GetReceptionStepCodeAsync(_unitOfWork, cancellationToken);

        var data = entities.Select(r => MapPendingItem(r, receptionStepCode)).ToList();

        return new PagedWarehouseAssignmentsDto<PendingAssignmentItemDto>
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static IQueryable<RecordEntrance> ApplyStepTwoCompletedFilter(
        IQueryable<RecordEntrance> source,
        DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.DUCA => source.Where(r =>
                r.ReceptionEntrance!.DocumentType == DocumentType.DUCA &&
                r.EntranceDucats.Any(d => d.DeletedAt == null) &&
                r.EntranceDucats.Where(d => d.DeletedAt == null).All(d => d.Status == DucaStatus.Completed)),
            DocumentType.CustomsDeclaration => source.Where(r =>
                r.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration &&
                r.CustomsDeclarations != null &&
                r.CustomsDeclarations.Details != null),
            _ => source.Where(_ => false)
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

    internal static PendingAssignmentItemDto MapPendingItem(RecordEntrance record, string receptionStepCode)
    {
        var reception = record.ReceptionEntrance!;
        var isDuca = reception.DocumentType == DocumentType.DUCA;
        var activeDucats = record.EntranceDucats.Where(d => d.DeletedAt == null).ToList();

        var arrivalLog = record.ExecutionLogs
            .FirstOrDefault(l => l.WorkflowStepDefinitionCode == receptionStepCode);

        return new PendingAssignmentItemDto
        {
            Id = record.Id,
            // PlateNumber = reception.PlateNumber,
            // PlateNumber = reception.PlateNumber,
            DriverName = reception.DriverName,
            DocumentType = reception.DocumentType,
            DocumentNumber = isDuca
                ? (activeDucats.Count > 0 ? activeDucats.First().DucatNumber : null)
                : record.CustomsDeclarations?.CustomsDeclarationNumber,
            // ContainerNumber = isDuca
            //     ? record.DucatRegistry?.ContainerNumber
            //     : record.CustomsDeclarations?.Details?.ContainerNumber,
            // ContainerNumber = isDuca
            //     ? record.DucatRegistry?.ContainerNumber
            //     : record.CustomsDeclarations?.Details?.ContainerNumber,
            ArrivalDate = arrivalLog?.StartDate,
            ArrivalTime = arrivalLog?.StartTime,
            TotalDocuments = isDuca
                ? activeDucats.Count
                : (record.CustomsDeclarations != null ? 1 : 0),
            CompletedDocuments = isDuca
                ? activeDucats.Count(d => d.Status == DucaStatus.Completed)
                : (record.CustomsDeclarations?.Details != null ? 1 : 0)
        };
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

        var query = _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Include(r => r.ReceptionEntrance!)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Warehouse)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Section)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Rack)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Lot)
            .Include(r => r.UnloadingDetails!)
                .ThenInclude(u => u.CrewAssignments)
            .Include(r => r.UnloadingDetails!)
                .ThenInclude(u => u.MachineryAssignments)
            .Include(r => r.ExecutionLogs)
            .Where(r => r.ReceptionEntrance != null
                && r.ReceptionEntrance.DeletedAt == null
                && r.DeletedAt == null
                && r.Assignment != null
                && r.Assignment.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.DriverName))
        {
            var driverFilter = request.DriverName.Trim().ToLower().Replace(" ", "");
            query = query.Where(r => r.ReceptionEntrance!.DriverName.ToLower().Replace(" ", "").Contains(driverFilter));
        }

        if (!string.IsNullOrWhiteSpace(request.PlateNumber))
        {
            var plateFilter = request.PlateNumber.Trim().ToLower().Replace(" ", "");
            // query = query.Where(r => r.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter));
        }
        // if (!string.IsNullOrWhiteSpace(request.PlateNumber))
        // {
        //     var plateFilter = request.PlateNumber.Trim().ToLower().Replace(" ", "");
        //     query = query.Where(r => r.ReceptionEntrance!.PlateNumber.ToLower().Replace(" ", "").Contains(plateFilter));
        // }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(r => r.Assignment!.AssignedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var data = entities.Select(record =>
        {
            var assignment = record.Assignment!;
            return new WarehouseAssignmentListItemDto
            {
                ReceptionId = record.Id,
                // PlateNumber = record.ReceptionEntrance!.PlateNumber,
                // DriverName = record.ReceptionEntrance.DriverName,
                // DocumentType = record.ReceptionEntrance.DocumentType,
                // PlateNumber = record.ReceptionEntrance!.PlateNumber,
                // DriverName = record.ReceptionEntrance.DriverName,
                // DocumentType = record.ReceptionEntrance.DocumentType,
                WarehouseName = assignment.Warehouse.WarehouseName,
                WarehouseType = assignment.Warehouse.WarehouseType,
                SectionCode = assignment.Section?.Code,
                RackCode = assignment.Rack?.Code,
                LotCode = assignment.Lot?.Code,
                AssignedAt = assignment.AssignedAt,
                IsCompleted = record.ExecutionLogs.Any(l =>
                    l.WorkflowStepDefinitionCode == WarehouseAssignmentRules.AssignmentStepCode && l.EndDate != null),
                CrewCount = record.UnloadingDetails?.CrewAssignments.Sum(c => c.PersonaCount) ?? 0,
                MachineryCount = record.UnloadingDetails?.MachineryAssignments.Count ?? 0
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

public class PagedWarehouseAssignmentsDto<T>
{
    public List<T> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}