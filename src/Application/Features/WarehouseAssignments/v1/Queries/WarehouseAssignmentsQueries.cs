using System;
using System.Collections.Generic;
using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries
{
    public class GetPendingWarehouseAssignmentsQuery : BaseRequest, IRequest<IEnumerable<PendingWarehouseAssignmentDto>>
    {
    }

    public class GetWarehouseStaffsQuery : BaseRequest, IRequest<IEnumerable<WarehouseStaffDto>>
    {
    }

    public class GetWarehouseAssignmentByIdQuery : BaseRequest, IRequest<WarehouseAssignmentDetailDto>
    {
        public Guid ReceptionId { get; set; }
        public Guid? EntranceDucatId { get; set; }
    }

    public class GetWarehouseAssignmentsHistoryQuery : BaseRequest, IRequest<IEnumerable<WarehouseAssignmentDetailDto>>
    {
    }
}
