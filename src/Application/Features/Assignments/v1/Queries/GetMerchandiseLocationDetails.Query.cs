using MediatR;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries
{
    public class GetMerchandiseLocationDetailsQuery : BaseRequest, IRequest<MerchandiseLocationDetailsDto>
    {
        public string? AssignmentCode { get; set; } 
    }
}