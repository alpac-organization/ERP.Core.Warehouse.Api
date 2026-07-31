using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;

public class GetMerchandiseRegistryQuery : IRequest<GetMerchandiseRegistryDto>
{
    public Guid CompanyId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}