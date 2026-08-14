using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;
using WarehouseAssignmentEntity = ERP.Core.Database.Domain.Entities.Warehouse.WarehouseAssignments;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public class GetWarehouseAssignmentDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetWarehouseAssignmentDetailQuery, WarehouseAssignmentDetailDto>(unitOfWork, errorManager)
{
    public override async Task<WarehouseAssignmentDetailDto> Handle(
        GetWarehouseAssignmentDetailQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var recordEntrance = await _unitOfWork.RecordEntrance.Entities
            .AsNoTracking()
            .Include(r => r.ReceptionEntrance!)
            .Include(r => r.EntranceDucats)
            .Include(r => r.DucatRegistry)
            .Include(r => r.CustomsDeclarations!)
                .ThenInclude(cd => cd.Details)
            .Include(r => r.ExecutionLogs)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Warehouse)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Section)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Rack)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.Lot)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.LotPosition)
            .Include(r => r.Assignment!)
                .ThenInclude(a => a.RackPosition)
            .Include(r => r.UnloadingDetails!)
                .ThenInclude(u => u.CrewAssignments)
            .Include(r => r.UnloadingDetails!)
                .ThenInclude(u => u.MachineryAssignments)
                    .ThenInclude(m => m.Machinery)
            .FirstOrDefaultAsync(r => r.Id == request.ReceptionId && r.DeletedAt == null, cancellationToken);

        if (recordEntrance == null || recordEntrance.ReceptionEntrance == null)
        {
            return _errorManager.ThrowNotFound<WarehouseAssignmentDetailDto>(
                "El registro de recepción no fue encontrado o ya ha sido eliminado.",
                "ERP:RECEPTION_NOT_FOUND");
        }

        var receptionStepCode = await GetPendingAssignmentsHandler.GetReceptionStepCodeAsync(_unitOfWork, cancellationToken);

        var detail = new WarehouseAssignmentDetailDto
        {
            Reception = GetPendingAssignmentsHandler.MapPendingItem(recordEntrance, receptionStepCode),
            Assignment = recordEntrance.Assignment == null ? null : MapAssignment(recordEntrance.Assignment),
            UnloadingDetails = recordEntrance.UnloadingDetails == null ? null : MapUnloadingDetails(recordEntrance.UnloadingDetails),
            Crew = recordEntrance.UnloadingDetails?.CrewAssignments
                .OrderByDescending(c => c.AssignedAt)
                .Select(c => new UnloadingCrewDto
                {
                    UnloadingCrewAssignmentId = c.Id,
                    PersonaCount = c.PersonaCount,
                    Tecerizada = c.Tecerizada,
                    AssignedAt = c.AssignedAt
                })
                .FirstOrDefault(),
            Machinery = recordEntrance.UnloadingDetails?.MachineryAssignments
                .Select(m => new UnloadingMachineryDto
                {
                    Id = m.Id,
                    MachineryId = m.MachineryCode,
                    MachineryName = m.Machinery.Name,
                    MachineryCode = m.Machinery.Code,
                    MachineryType = m.Machinery.MachineryType,
                    StartTime = m.StartTime,
                    EndTime = m.EndTime
                })
                .ToList() ?? []
        };

        return detail;
    }

    private static WarehouseAssignmentDto MapAssignment(WarehouseAssignmentEntity assignment)
    {
        return new WarehouseAssignmentDto
        {
            WarehouseId = assignment.WarehouseId,
            WarehouseName = assignment.Warehouse.WarehouseName,
            WarehouseCode = assignment.Warehouse.Code,
            WarehouseType = assignment.Warehouse.WarehouseType,
            SectionId = assignment.SectionId,
            SectionCode = assignment.Section?.Code,
            RackId = assignment.RackId,
            RackCode = assignment.Rack?.Code,
            LotsId = assignment.LotsId,
            LotsPositionsId = assignment.LotsPositionsId,
            RackPositionsId = assignment.RackPositionsId,
            AssignedAt = assignment.AssignedAt,
            AssignedByUserId = assignment.AssignedByUserId
        };
    }

    private static UnloadingDetailsDto MapUnloadingDetails(UnloadingDetails details)
    {
        return new UnloadingDetailsDto
        {
            UnloadingDetailsId = details.Id,
            UnloadingStartTime = details.UnloadingStartTime,
            UnloadingEndTime = details.UnloadingEndTime,
            WarehouseChiefUserId = details.WarehouseChiefUserId,
            PreparedPallets = details.PreparedPallets
        };
    }
}