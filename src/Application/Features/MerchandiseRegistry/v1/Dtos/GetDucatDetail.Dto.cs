using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

public class GetDucatDetailDto
{
    public DucaType Type { get; set; }
    public string? MerchandiseName { get; set; }
    public int? TotalBultos { get; set; }
    public decimal? TotalWeight { get; set; }
    public string? MerchandiseDescription { get; set; }
    public string? Sender { get; set; }
}
