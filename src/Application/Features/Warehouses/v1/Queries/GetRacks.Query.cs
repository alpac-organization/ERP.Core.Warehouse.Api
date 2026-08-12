using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

public class GetRackSectionSummaryQuery : BaseRequest, IRequest<RackSectionSummaryDto>
{
    public Guid SectionId { get; set; }
}