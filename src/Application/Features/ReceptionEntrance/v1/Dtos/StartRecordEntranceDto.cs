namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record StartRecordEntranceResponse(
    Guid RecordEntranceManaguaId,
    string Status
);