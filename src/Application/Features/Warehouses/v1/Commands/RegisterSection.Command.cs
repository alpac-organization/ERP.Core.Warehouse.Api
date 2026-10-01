using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

public class RegisterSectionCommand : BaseRequest, IRequest<bool>, IHasCoordinates
{
    [JsonIgnore]
    public Guid WarehouseId { get; set; }
    public SectionType SectionType { get; set; }
    public SectionStorageType SectionStorageType { get; set; }
    public decimal Width { get; set; }
    public decimal Length { get; set; }
    public int? MaximumNumberOfPalletsPerLevel { get; set; }
    public decimal? PositionX { get; set; }
    public decimal? PositionY { get; set; }
    public decimal? PositionZ { get; set; }
    public decimal? RotationY { get; set; }
}