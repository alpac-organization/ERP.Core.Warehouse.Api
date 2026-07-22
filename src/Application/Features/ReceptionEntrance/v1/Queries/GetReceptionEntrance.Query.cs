using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

public class GetReceptionEntrancesQuery : IRequest<GetReceptionEntrancesDto>
{
    public Guid CompanyId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public string? DriverName { get; set; }
    public string? PlateNumber { get; set; }
    public string? DucatNumber { get; set; }
    public Guid? DucatId { get; set; }
    public DateTime? Date { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}