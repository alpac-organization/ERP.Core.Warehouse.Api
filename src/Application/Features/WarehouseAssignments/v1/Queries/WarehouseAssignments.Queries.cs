using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;

public class GetPendingAssignmentsQuery : BaseRequest, IRequest<PagedWarehouseAssignmentsDto<PendingAssignmentItemDto>>
{
    public string? DriverName { get; set; }
    public string? PlateNumber { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetWarehouseAssignmentsQuery : BaseRequest, IRequest<PagedWarehouseAssignmentsDto<WarehouseAssignmentListItemDto>>
{
    public string? DriverName { get; set; }
    public string? PlateNumber { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetWarehouseAssignmentDetailQuery : BaseRequest, IRequest<WarehouseAssignmentDetailDto>
{
    public Guid ReceptionId { get; set; }
}

public class GetAvailableWarehousesQuery : BaseRequest, IRequest<List<AvailableWarehouseDto>>
{
    public DocumentType? DocumentType { get; set; }
    public Guid? RackId { get; set; }
    public Guid? LotId { get; set; }
}

public class GetWarehouseMachineriesQuery : BaseRequest, IRequest<List<WarehouseMachineryDto>>
{
}

public class GetWarehouseStaffsQuery : BaseRequest, IRequest<List<WarehouseStaffDto>>
{
}