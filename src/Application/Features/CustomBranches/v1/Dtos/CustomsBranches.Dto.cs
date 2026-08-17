namespace ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Dtos;

public record CustomBranchListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}