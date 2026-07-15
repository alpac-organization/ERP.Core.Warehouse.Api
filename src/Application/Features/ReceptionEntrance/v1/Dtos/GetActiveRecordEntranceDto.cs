namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

public record GetActiveRecordEntranceResponse
{
    public bool HasPending { get; set; }
    public Guid? RecordEntranceId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CurrentStepId { get; set; }
}