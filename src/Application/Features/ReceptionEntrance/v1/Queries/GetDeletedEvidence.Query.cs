using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

public class GetDeletedEvidencesQuery : BaseRequest, IRequest<GetDeletedEvidencesDto>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}