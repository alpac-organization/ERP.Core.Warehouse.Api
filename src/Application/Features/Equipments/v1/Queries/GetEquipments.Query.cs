using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Queries;

public class GetEquipmentsQuery : BaseRequest, IRequest<PagedResponse<EquipmentDto>>
{
    public string? EquipmentBrand { get; set; }
    public string? EquipmentModel { get; set; }
    public string? EquipmentCode { get; set; }
    public EquipmentStatus? Status { get; set; }
    public EquipmentType? Type { get; set; }
    public Guid? BranchId { get; set; }
    public bool? IsActive { get; set; }
    public bool? Gasoline { get; set; }
    public bool? Diesel { get; set; }
    public bool? Electric { get; set; }
    public bool? Lpg { get; set; }

    public int PageSize { get; set; }
    public int PageNumber { get; set; }
}