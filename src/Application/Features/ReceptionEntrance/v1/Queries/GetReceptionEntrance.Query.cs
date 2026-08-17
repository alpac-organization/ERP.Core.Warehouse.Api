using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

public class GetReceptionEntrancesQuery : IRequest<GetReceptionEntrancesDto>
{
    public Guid CompanyId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }

    public string? DriverName { get; set; }
    public string? PlateNumber { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public String? DucatNumber { get; set; }
    public Guid? DucatId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetReceptionEntranceDetailQuery : IRequest<ReceptionEntranceDetailDto>
{
    public Guid CompanyId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid RecordId { get; set; }
}