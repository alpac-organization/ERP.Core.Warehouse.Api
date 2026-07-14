using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

public class StartRecordEntranceCommand : BaseRequest, IRequest<StartRecordEntranceResponse>
{
    public Guid? ServiceOrderId { get; set; }
    public int CurrentStepId { get; set; }
    public bool IsConsolidated { get; set; }
}