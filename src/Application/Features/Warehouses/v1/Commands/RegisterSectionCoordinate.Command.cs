using MediatR;
using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterSectionCoordinateCommand : BaseRequest, IRequest<bool>
{
   public Guid WarehouseId { get; set; }
   public Guid SectionId { get; set; }
   public decimal PositionX { get; set; }
   public decimal PositionY { get; set; }
   public decimal PositionZ { get; set; }
   public decimal RotationY { get; set; }
}