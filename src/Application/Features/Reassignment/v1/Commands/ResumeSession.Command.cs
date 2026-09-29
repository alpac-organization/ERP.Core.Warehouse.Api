using MediatR;
using System.Text.Json.Serialization;

using ERP.Core.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands
{
    public class ResumeSessionCommand : BaseRequest, IRequest<bool>
    {
        [JsonIgnore]
        public Guid SessionId { get; set; }
    }
}