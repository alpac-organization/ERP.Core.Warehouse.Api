using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;

public class CreateWarehouseAssignmentCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? SectionId { get; set; }
    public Guid RackId { get; set; }
    public Guid? LotsId { get; set; }
    public Guid? LotsPositionsId { get; set; }
    public Guid? RackPositionsId { get; set; }
}

public class CreateUnloadingDetailsCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public DateTime UnloadingStartTime { get; set; }
    public string WarehouseChiefUserId { get; set; } = null!;
    public decimal? PreparedPallets { get; set; }
}

public class CreateUnloadingCrewCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public int PersonaCount { get; set; }
    public bool Tecerizada { get; set; }
}

public class CreateUnloadingMachineryCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
    public Guid MachineryCode { get; set; }
    public DateTime StartTime { get; set; }
}

public class CompleteWarehouseAssignmentCommand : BaseRequest, IRequest<bool>
{
    public Guid ReceptionId { get; set; }
}