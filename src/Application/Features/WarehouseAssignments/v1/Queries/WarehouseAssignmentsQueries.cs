using System;
using System.Collections.Generic;
using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries
{
    public class GetPendingWarehouseAssignmentsQuery : BaseRequest, IRequest<PagedResponse<PendingWarehouseAssignmentDto>>
    {
        public string? DriverName { get; set; }
        public string? LicensePlate { get; set; }
        public DocumentType? DocumentType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }


    public class GetWarehouseAssignmentByIdQuery : BaseRequest, IRequest<WarehouseAssignmentDetailDto>
    {
        public Guid ReceptionId { get; set; }
        public Guid? EntranceDucatId { get; set; }
    }

    public class GetWarehouseAssignmentsHistoryQuery : BaseRequest, IRequest<PagedResponse<WarehouseAssignmentDetailDto>>
    {
        public string? DriverName { get; set; }
        public string? LicensePlate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
