namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos
{
    public class ReceptionEntranceListResponseDto
    {
        public List<ReceptionEntranceDto> Data { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public ReceptionEntranceDto.Stats Stats { get; set; } = new();
    }
}