using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using WarehouseAssignmentEntity = ERP.Core.Database.Domain.Entities.Warehouse.WarehouseAssignments;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers
{
    public class GetPendingWarehouseAssignmentsHandler : BaseValidatorHandler<GetPendingWarehouseAssignmentsQuery, PagedResponse<PendingWarehouseAssignmentDto>>
    {
        public GetPendingWarehouseAssignmentsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<PagedResponse<PendingWarehouseAssignmentDto>> Handle(GetPendingWarehouseAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var baseQuery = BuildPendingBaseQuery(request);

            var totalCount = await baseQuery.CountAsync(cancellationToken);
            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;

            var pagedRecords = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    LicensePlate = r.ReceptionEntrance!.VehiclePlateNumber,
                    DriverName = r.ReceptionEntrance.DriverName,
                    EntranceTime = r.CreatedAt,
                    Status = r.CurrentStepCode,
                    r.IsConsolidated,
                    DocumentType = r.ReceptionEntrance.DocumentType,
                    CustomsNumber = r.CustomsDeclarations != null ? r.CustomsDeclarations.CustomsDeclarationNumber : null,
                    CustomsOrderCode = r.CustomsDeclarations != null ? r.CustomsDeclarations.ServiceOrderCode : null,
                    CustomsStatus = r.CustomsDeclarations != null ? r.CustomsDeclarations.Status : DucaStatus.Pending,
                    ActiveDucats = r.EntranceDucats
                        .Where(d => d.DeletedAt == null && d.Status == DucaStatus.Completed)
                        .Select(d => new
                        {
                            d.Id,
                            d.DucatNumber,
                            d.Status,
                            d.ServiceOrderCode,
                            AlreadyAssigned = _unitOfWork.WarehouseAssignments.Entities
                                .Any(a => a.RecordEntranceId == r.Id && a.EntranceDucatId == d.Id && a.DeletedAt == null)
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            var data = new List<PendingWarehouseAssignmentDto>();

            foreach (var r in pagedRecords)
            {
                if (r.DocumentType == DocumentType.DUCA)
                {
                    foreach (var d in r.ActiveDucats.Where(x => !x.AlreadyAssigned))
                    {
                        data.Add(new PendingWarehouseAssignmentDto
                        {
                            ReceptionId = r.Id,
                            LicensePlate = r.LicensePlate ?? "N/A",
                            DriverName = r.DriverName ?? "N/A",
                            EntranceTime = r.EntranceTime,
                            Status = d.Status.ToString(),
                            IsConsolidated = r.IsConsolidated,
                            EntranceDucatId = d.Id,
                            DocumentType = "DUCA",
                            DocumentNumber = d.DucatNumber ?? "N/A",
                            ServiceOrderCode = d.ServiceOrderCode
                        });
                    }
                }
                else if (r.DocumentType == DocumentType.CustomsDeclaration)
                {
                    data.Add(new PendingWarehouseAssignmentDto
                    {
                        ReceptionId = r.Id,
                        LicensePlate = r.LicensePlate ?? "N/A",
                        DriverName = r.DriverName ?? "N/A",
                        EntranceTime = r.EntranceTime,
                        Status = r.CustomsStatus.ToString(),
                        IsConsolidated = r.IsConsolidated,
                        EntranceDucatId = null,
                        DocumentType = "Declaración Aduanera",
                        DocumentNumber = r.CustomsNumber ?? "N/A",
                        ServiceOrderCode = r.CustomsOrderCode
                    });
                }
            }

            return new PagedResponse<PendingWarehouseAssignmentDto>(data, pageNumber, pageSize, totalCount);
        }

        private IQueryable<RecordEntrance> BuildPendingBaseQuery(GetPendingWarehouseAssignmentsQuery request)
        {
            var baseQuery = _unitOfWork.RecordEntrance.Entities
                .AsNoTracking()
                .Where(r => r.DeletedAt == null 
                         && r.ReceptionEntrance != null 
                         && r.ReceptionEntrance.DeletedAt == null);

            // Filtrar recepciones que tengan documentos individuales completados y pendientes de asignación
            baseQuery = baseQuery.Where(r => 
                (r.ReceptionEntrance!.DocumentType == DocumentType.DUCA 
                    && r.EntranceDucats.Any(d => d.DeletedAt == null 
                        && d.Status == DucaStatus.Completed
                        && !_unitOfWork.WarehouseAssignments.Entities.Any(a => a.RecordEntranceId == r.Id && a.EntranceDucatId == d.Id && a.DeletedAt == null)))
                ||
                (r.ReceptionEntrance.DocumentType == DocumentType.CustomsDeclaration 
                    && r.CustomsDeclarations != null 
                    && r.CustomsDeclarations.Details != null
                    && r.CustomsDeclarations.Status == DucaStatus.Completed
                    && !_unitOfWork.WarehouseAssignments.Entities.Any(a => a.RecordEntranceId == r.Id && a.EntranceDucatId == null && a.DeletedAt == null))
            );

            if (!string.IsNullOrWhiteSpace(request.DriverName))
            {
                var driverSearch = request.DriverName.Trim().ToLower();
                baseQuery = baseQuery.Where(r => EF.Functions.Like(r.ReceptionEntrance!.DriverName.ToLower(), $"%{driverSearch}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.LicensePlate))
            {
                var plateSearch = request.LicensePlate.Trim().ToLower();
                baseQuery = baseQuery.Where(r => EF.Functions.Like(r.ReceptionEntrance!.VehiclePlateNumber.ToLower(), $"%{plateSearch}%"));
            }

            if (request.DocumentType.HasValue)
            {
                baseQuery = baseQuery.Where(r => r.ReceptionEntrance!.DocumentType == request.DocumentType.Value);
            }

            return baseQuery;
        }
    }


    public class GetWarehouseAssignmentByIdHandler : BaseValidatorHandler<GetWarehouseAssignmentByIdQuery, WarehouseAssignmentDetailDto>
    {
        public GetWarehouseAssignmentByIdHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<WarehouseAssignmentDetailDto> Handle(GetWarehouseAssignmentByIdQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var record = await _unitOfWork.RecordEntrance.Entities
                .AsNoTracking()
                .Include(r => r.ReceptionEntrance)
                .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

            if (record == null)
            {
                return _errorManager.ThrowBadRequest<WarehouseAssignmentDetailDto>(
                    "El registro de recepcion no fue encontrado.",
                    "ERP:RECEPTION_NOT_FOUND");
            }

            var assignment = await GetActiveAssignmentAsync(request.ReceptionId, request.EntranceDucatId, cancellationToken);
            var collaboratorsDict = await GetCollaboratorsDictAsync(assignment, cancellationToken);

            var dto = new WarehouseAssignmentDetailDto
            {
                ReceptionId = record.Id,
                AssignmentId = assignment?.Id,
                LicensePlate = record.ReceptionEntrance?.VehiclePlateNumber ?? "N/A",
                WarehouseName = assignment?.Warehouse?.WarehouseName,
                DucatNumber = assignment?.EntranceDucat?.DucatNumber,
                ServiceOrderCode = assignment?.EntranceDucat?.ServiceOrderCode,
                UnloadingStartTime = assignment?.UnloadingStartTime,
                UnloadingEndTime = assignment?.UnloadingEndTime
            };

            if (assignment != null)
            {
                dto.Crews = MapCrewGroups(assignment.CrewAssignments, collaboratorsDict);
                dto.Machineries = MapMachineries(assignment.MachineryAssignments, collaboratorsDict);
            }

            return dto;
        }

        private async Task<WarehouseAssignmentEntity?> GetActiveAssignmentAsync(
            Guid receptionId, Guid? entranceDucatId, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.WarehouseAssignments.Entities
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Warehouse)
                .Include(a => a.EntranceDucat)
                .Include(a => a.CrewAssignments)
                .Include(a => a.MachineryAssignments)
                    .ThenInclude(m => m.Machinery)
                .Where(a => a.RecordEntranceId == receptionId && a.DeletedAt == null);

            query = entranceDucatId.HasValue
                ? query.Where(a => a.EntranceDucatId == entranceDucatId.Value)
                : query.Where(a => a.EntranceDucatId == null);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<Dictionary<Guid, string>> GetCollaboratorsDictAsync(
            WarehouseAssignmentEntity? assignment, CancellationToken cancellationToken)
        {
            if (assignment == null) return new Dictionary<Guid, string>();

            var crewCollaboratorIds = assignment.CrewAssignments
                .Where(c => c.CollaboratorId.HasValue && c.DeletedAt == null)
                .Select(c => c.CollaboratorId!.Value);

            var operatorCollaboratorIds = assignment.MachineryAssignments
                .Where(m => m.OperatorCollaboratorId.HasValue && m.DeletedAt == null)
                .Select(m => m.OperatorCollaboratorId!.Value);

            var allCollaboratorIds = crewCollaboratorIds.Union(operatorCollaboratorIds).Distinct().ToList();
            if (allCollaboratorIds.Count == 0) return new Dictionary<Guid, string>();

            return await _unitOfWork.Collaborators.Entities
                .AsNoTracking()
                .Where(c => allCollaboratorIds.Contains(c.Id))
                .ToDictionaryAsync(
                    c => c.Id,
                    c => ((c.FirstName ?? "") + " " + (c.FirstLastname ?? "")).Trim(),
                    cancellationToken);
        }

        private static List<WarehouseCrewGroupDto> MapCrewGroups(
            ICollection<CrewAssignments>? crewAssignments, Dictionary<Guid, string> collaboratorsDict)
        {
            if (crewAssignments == null || crewAssignments.Count == 0) return new List<WarehouseCrewGroupDto>();

            return crewAssignments
                .Where(c => c.DeletedAt == null)
                .GroupBy(c => new { c.IsOutsourced, c.ProviderName, c.InvoiceNumber })
                .Select(g => new WarehouseCrewGroupDto
                {
                    IsOutsourced = g.Key.IsOutsourced,
                    ProviderName = g.Key.ProviderName,
                    InvoiceNumber = g.Key.InvoiceNumber,
                    TotalPersonCount = g.Sum(x => x.PersonCount ?? 1),
                    CollaboratorIds = g.Where(x => x.CollaboratorId != null).Select(x => x.CollaboratorId!.Value).ToList(),
                    CollaboratorNames = g.Where(x => x.CollaboratorId != null && collaboratorsDict.ContainsKey(x.CollaboratorId!.Value))
                                         .Select(x => collaboratorsDict[x.CollaboratorId!.Value])
                                         .ToList(),
                    CrewAssignmentIds = g.Select(x => x.Id).ToList()
                }).ToList();
        }

        private static List<WarehouseMachineryDetailDto> MapMachineries(
            ICollection<MachineryAssignments>? machineryAssignments, Dictionary<Guid, string> collaboratorsDict)
        {
            if (machineryAssignments == null || machineryAssignments.Count == 0) return new List<WarehouseMachineryDetailDto>();

            return machineryAssignments
                .Where(m => m.DeletedAt == null)
                .Select(m => new WarehouseMachineryDetailDto
                {
                    MachineryAssignmentId = m.Id,
                    IsOutsourced = m.IsOutsourced,
                    MachineryId = m.MachineryId,
                    MachineryCode = !m.IsOutsourced && m.Machinery != null ? m.Machinery.Code : null,
                    MachineryName = !m.IsOutsourced && m.Machinery != null ? m.Machinery.Name : null,
                    OperatorCollaboratorId = m.OperatorCollaboratorId,
                    OperatorName = m.OperatorCollaboratorId.HasValue && collaboratorsDict.TryGetValue(m.OperatorCollaboratorId.Value, out var opName) 
                        ? opName 
                        : null,
                    ProviderName = m.ProviderName,
                    InvoiceNumber = m.InvoiceNumber,
                    MachineryDescription = m.MachineryDescription,
                    StartTime = m.StartTime,
                    EndTime = m.EndTime
                }).ToList();
        }
    }

    public class GetWarehouseAssignmentsHistoryHandler : BaseValidatorHandler<GetWarehouseAssignmentsHistoryQuery, PagedResponse<WarehouseAssignmentDetailDto>>
    {
        public GetWarehouseAssignmentsHistoryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<PagedResponse<WarehouseAssignmentDetailDto>> Handle(GetWarehouseAssignmentsHistoryQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var baseQuery = _unitOfWork.WarehouseAssignments.Entities
                .AsNoTracking()
                .Where(a => a.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(request.DriverName))
            {
                var driverSearch = request.DriverName.Trim().ToLower();
                baseQuery = baseQuery.Where(a => a.RecordEntrance.ReceptionEntrance != null 
                                              && EF.Functions.Like(a.RecordEntrance.ReceptionEntrance.DriverName.ToLower(), $"%{driverSearch}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.LicensePlate))
            {
                var plateSearch = request.LicensePlate.Trim().ToLower();
                baseQuery = baseQuery.Where(a => a.RecordEntrance.ReceptionEntrance != null 
                                              && EF.Functions.Like(a.RecordEntrance.ReceptionEntrance.VehiclePlateNumber.ToLower(), $"%{plateSearch}%"));
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;

            var data = await baseQuery
                .OrderByDescending(a => a.AssignedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new WarehouseAssignmentDetailDto
                {
                    ReceptionId = a.RecordEntranceId,
                    AssignmentId = a.Id,
                    LicensePlate = a.RecordEntrance.ReceptionEntrance != null 
                        ? (a.RecordEntrance.ReceptionEntrance.VehiclePlateNumber ?? "N/A") 
                        : "N/A",
                    WarehouseName = a.Warehouse != null ? a.Warehouse.WarehouseName : "N/A",
                    DucatNumber = a.EntranceDucat != null ? a.EntranceDucat.DucatNumber : null,
                    ServiceOrderCode = a.EntranceDucat != null ? a.EntranceDucat.ServiceOrderCode : null,
                    UnloadingStartTime = a.UnloadingStartTime,
                    UnloadingEndTime = a.UnloadingEndTime
                })
                .ToListAsync(cancellationToken);

            return new PagedResponse<WarehouseAssignmentDetailDto>(data, pageNumber, pageSize, totalCount);
        }
    }
}
