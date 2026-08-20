public class DeletedEvidenceListItemDto
{
    public Guid RecordEntranceId { get; set; }
    public List<string> DeletedEvidenceUrls { get; set; } = [];
}

public class GetDeletedEvidencesDto
{
    public List<DeletedEvidenceListItemDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}