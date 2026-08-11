namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

public class MerchandiseDto
{
    public Guid MerchandiseId { get; set; }
    public string MerchandiseName { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }

    public MerchandiseCategoryDto Category { get; set; } = default!;
}


public class MerchandiseCategoryDto
{
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public bool IsActive { get; set; }
}

public class RegisterMerchandiseDto
{
    public string MerchandiseName { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
}