using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries
{
    public class GetReceptionEntranceDetailsQuery : BaseRequest, IRequest<ReceptionEntranceDetailsDto>
    {
        [JsonIgnore]
        public Guid ReceptionEntranceId { get; set; }
    }
}