using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class AssignMerchandiseDesignatedLocationCommand : BaseRequest, IRequest<AssignMerchandiseDesignatedLocationDto>, IAssignmentOperationalRequest
    {
        [JsonIgnore]
        public Guid OperationalOrderId { get; set; }

        [JsonIgnore]
        public Guid AssignmentOperationalId { get; set; }

        public List<AssignMerchandiseSectionDto> Sections { get; set; } = [];
    }

    public class AssignMerchandiseSectionDto
    {
        public Guid SectionId { get; set; }
        public List<AssignMerchandiseBlockDto> Tramos { get; set; } = [];
        public List<AssignMerchandiseBlockDto> Racks { get; set; } = [];
    }

    public class AssignMerchandiseBlockDto
    {
        public Guid BlockId { get; set; }
        public List<Guid> PositionIds { get; set; } = [];
    }
}