namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos
{
    public class UnitMeasureDatailsDto
    {
        public Guid UnitMeasureId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }   
    }
}