using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
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

        var context = await WarehouseDocumentLookup.FindDocumentAsync(
            _unitOfWork, request.DocumentId, request.DocumentType, cancellationToken);

        if (context == null || context.RecordEntrance.ReceptionEntrance == null)
        {
            return _errorManager.ThrowNotFound<WarehouseAssignmentDetailDto>(
                "El documento no fue encontrado o ya ha sido eliminado.",
                "ERP:DOCUMENT_NOT_FOUND");
        }

        var assignment = await _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .Include(a => a.Warehouse)
            .Include(a => a.Section)
            .Include(a => a.Rack)
            .Include(a => a.Lot)
            .Include(a => a.LotPosition)
            .Include(a => a.RackPosition)
            .Include(a => a.UnloadingDetails!)
                .ThenInclude(u => u.CrewAssignments)
            .Include(a => a.UnloadingDetails!)
                .ThenInclude(u => u.MachineryAssignments)
                    .ThenInclude(m => m.Machinery)
            .FirstOrDefaultAsync(a => a.DeletedAt == null &&
                (request.DocumentType == DocumentType.DUCA
                    ? a.EntranceDucatId == request.DocumentId
                    : a.CustomsDeclarationId == request.DocumentId), cancellationToken);

        var reception = context.RecordEntrance.ReceptionEntrance!;
        var receptionStepCode = await GetPendingAssignmentsHandler.GetReceptionStepCodeAsync(_unitOfWork, cancellationToken);
        var arrivalLog = context.RecordEntrance.ExecutionLogs
            .FirstOrDefault(l => l.WorkflowStepDefinitionCode == receptionStepCode);

        var detail = new WarehouseAssignmentDetailDto
        {
            Id = context.DocumentId,
            ReceptionId = context.RecordEntrance.Id,
            DocumentType = context.DocumentType,
            DocumentNumber = WarehouseDocumentLookup.GetDocumentNumber(context),
            ServiceOrderCode = WarehouseDocumentLookup.GetServiceOrderCode(context),
            MerchandiseName = WarehouseDocumentLookup.GetMerchandiseName(context),
            TotalBultos = WarehouseDocumentLookup.GetTotalBultos(context),
            TotalWeight = WarehouseDocumentLookup.GetTotalWeight(context),
            Remitente = WarehouseDocumentLookup.GetRemitente(context),
            PlateNumber = reception.PlateNumber,
            DriverName = reception.DriverName,
            ContainerNumber = WarehouseDocumentLookup.GetContainerNumber(context),
            ArrivalDate = arrivalLog?.StartDate,
            ArrivalTime = arrivalLog?.StartTime,
            Assignment = assignment == null ? null : MapAssignment(assignment),
            UnloadingDetails = assignment?.UnloadingDetails == null ? null : MapUnloadingDetails(assignment.UnloadingDetails),
            Crew = assignment?.UnloadingDetails?.CrewAssignments
                .OrderByDescending(c => c.AssignedAt)
                .Select(c => new UnloadingCrewDto
                {
                    UnloadingCrewAssignmentId = c.Id,
                    PersonaCount = c.PersonaCount,
                    Tecerizada = c.Tecerizada,
                    AssignedAt = c.AssignedAt
                })
                .FirstOrDefault(),
            Machinery = assignment?.UnloadingDetails?.MachineryAssignments
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
