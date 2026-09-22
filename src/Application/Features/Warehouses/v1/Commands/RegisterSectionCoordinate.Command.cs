using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using MediatR;

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