namespace ERP.Core.Warehouse.Api.Domain.Entities.Bases
{
    public record PagedResponse<T>(
        List<T> Data, 
        
        int PageNumber, 
        int PageSize,

        int Total = 0
    );
}