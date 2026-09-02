using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers
{
    public class GetPendingWarehouseAssignmentsHandler : BaseValidatorHandler<GetPendingWarehouseAssignmentsQuery, IEnumerable<PendingWarehouseAssignmentDto>>
    {
        public GetPendingWarehouseAssignmentsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<IEnumerable<PendingWarehouseAssignmentDto>> Handle(GetPendingWarehouseAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var records = await _unitOfWork.RecordEntrance.Entities
                .AsNoTracking()
                .Include(r => r.ReceptionEntrance)
                .Include(r => r.EntranceDucats)
                .Include(r => r.CustomsDeclarations!)
                    .ThenInclude(cd => cd.Details)
                .Where(r => r.DeletedAt == null && r.ReceptionEntrance != null && r.ReceptionEntrance.DeletedAt == null)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            var recordIds = records.Select(r => r.Id).ToList();

            var existingAssignments = await _unitOfWork.WarehouseAssignments.Entities
                .AsNoTracking()
                .Where(a => recordIds.Contains(a.RecordEntranceId) && a.DeletedAt == null)
                .Select(a => new { a.RecordEntranceId, a.EntranceDucatId })
                .ToListAsync(cancellationToken);

            var pendingList = new List<PendingWarehouseAssignmentDto>();

            foreach (var r in records)
            {
                if (!WarehouseAssignmentRules.IsStepTwoCompleted(r))
                {
                    continue;
                }

                var isDuca = r.ReceptionEntrance?.DocumentType == DocumentType.DUCA;

                if (isDuca)
                {
                    var assignedDucatIds = existingAssignments
                        .Where(a => a.RecordEntranceId == r.Id && a.EntranceDucatId.HasValue)
                        .Select(a => a.EntranceDucatId!.Value)
                        .ToHashSet();

                    var activeDucats = r.EntranceDucats.Where(d => d.DeletedAt == null).ToList();

                    if (activeDucats.All(d => assignedDucatIds.Contains(d.Id)))
                    {
                        continue;
                    }

                    pendingList.Add(new PendingWarehouseAssignmentDto
                    {
                        ReceptionId = r.Id,
                        LicensePlate = r.ReceptionEntrance?.VehiclePlateNumber ?? "N/A",
                        DriverName = r.ReceptionEntrance?.DriverName ?? "N/A",
                        EntranceTime = r.CreatedAt,
                        Status = r.CurrentStepCode ?? "N/A",
                        IsConsolidated = r.IsConsolidated,
                        Ducas = activeDucats.Select(d => new PendingDucaDto
                        {
                            EntranceDucatId = d.Id,
                            DucatNumber = d.DucatNumber,
                            Status = d.Status.ToString(),
                            ServiceOrderCode = d.ServiceOrderCode,
                            AlreadyAssigned = assignedDucatIds.Contains(d.Id)
                        }).ToList()
                    });
                }
                else
                {
                    var alreadyAssigned = existingAssignments.Any(a => a.RecordEntranceId == r.Id && a.EntranceDucatId == null);
                    if (alreadyAssigned)
                    {
                        continue;
                    }

                    pendingList.Add(new PendingWarehouseAssignmentDto
                    {
                        ReceptionId = r.Id,
                        LicensePlate = r.ReceptionEntrance?.VehiclePlateNumber ?? "N/A",
                        DriverName = r.ReceptionEntrance?.DriverName ?? "N/A",
                        EntranceTime = r.CreatedAt,
                        Status = r.CurrentStepCode ?? "N/A",
                        IsConsolidated = false,
                        Ducas = new List<PendingDucaDto>()
                    });
                }
            }

            return pendingList;
        }
    }

    public class GetWarehouseStaffsHandler : BaseValidatorHandler<GetWarehouseStaffsQuery, IEnumerable<WarehouseStaffDto>>
    {
        public GetWarehouseStaffsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<IEnumerable<WarehouseStaffDto>> Handle(GetWarehouseStaffsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var collaborators = await _unitOfWork.Collaborators.Entities
                .AsNoTracking()
                .Where(c => c.CompanyId == request.CompanyId && c.DeletedAt == null && c.Status == CollaboratorStatus.Active)
                .OrderBy(c => c.FirstName)
                .Select(c => new WarehouseStaffDto
                {
                    UserId = c.Id,
                    Fullname = ((c.FirstName ?? "") + " " + (c.FirstLastname ?? "")).Trim()
                })
                .ToListAsync(cancellationToken);

            return collaborators;
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
                    "El registro de recepción no fue encontrado.",
                    "ERP:RECEPTION_NOT_FOUND");
            }

            var assignmentQuery = _unitOfWork.WarehouseAssignments.Entities
                .AsNoTracking()
                .Include(a => a.Warehouse)
                .Include(a => a.EntranceDucat)
                .Include(a => a.CrewAssignments)
                .Include(a => a.MachineryAssignments)
                .Where(a => a.RecordEntranceId == request.ReceptionId && a.DeletedAt == null);

            if (request.EntranceDucatId.HasValue)
            {
                assignmentQuery = assignmentQuery.Where(a => a.EntranceDucatId == request.EntranceDucatId.Value);
            }
            else
            {
                assignmentQuery = assignmentQuery.Where(a => a.EntranceDucatId == null);
            }

            var assignment = await assignmentQuery.FirstOrDefaultAsync(cancellationToken);

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
                if (assignment.CrewAssignments != null && assignment.CrewAssignments.Any())
                {
                    dto.Crews = assignment.CrewAssignments
                        .Where(c => c.DeletedAt == null)
                        .GroupBy(c => new { c.IsOutsourced, c.ProviderName })
                        .Select(g => new WarehouseCrewGroupDto
                        {
                            IsOutsourced = g.Key.IsOutsourced,
                            ProviderName = g.Key.ProviderName,
                            TotalPersonCount = g.Sum(x => x.PersonCount ?? 1),
                            CollaboratorIds = g.Where(x => x.CollaboratorId != null).Select(x => x.CollaboratorId!.ToString()!).ToList(),
                            CrewAssignmentIds = g.Select(x => x.Id).ToList()
                        }).ToList();
                }

                if (assignment.MachineryAssignments != null)
                {
                    dto.Machineries = assignment.MachineryAssignments
                        .Where(m => m.DeletedAt == null)
                        .Select(m => new WarehouseMachineryDetailDto
                        {
                            MachineryAssignmentId = m.Id,
                            IsOutsourced = m.IsOutsourced,
                            MachineryCode = m.MachineryCode?.ToString(),
                            OperatorName = m.OperatorCollaboratorId?.ToString(),
                            ProviderName = m.ProviderName
                        }).ToList();
                }
            }

            return dto;
        }
    }

    public class GetWarehouseAssignmentsHistoryHandler : BaseValidatorHandler<GetWarehouseAssignmentsHistoryQuery, IEnumerable<WarehouseAssignmentDetailDto>>
    {
        public GetWarehouseAssignmentsHistoryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<IEnumerable<WarehouseAssignmentDetailDto>> Handle(GetWarehouseAssignmentsHistoryQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var assignments = await _unitOfWork.WarehouseAssignments.Entities
                .AsNoTracking()
                .Include(a => a.RecordEntrance)
                    .ThenInclude(r => r.ReceptionEntrance)
                .Include(a => a.EntranceDucat)
                .Include(a => a.Warehouse)
                .Where(a => a.DeletedAt == null)
                .OrderByDescending(a => a.AssignedAt)
                .Take(50)
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

            return assignments;
        }
    }
}
