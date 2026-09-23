namespace ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Dtos
{
    public class CustomsBranchDto
    {
        public Guid CustomBranchId { get; set; }
        public string? Code { get; set; }
        public string? CustomsBranchName { get; set; }
    }
}