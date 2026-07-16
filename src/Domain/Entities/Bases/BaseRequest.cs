using System.Text.Json.Serialization;

namespace ERP.Core.Warehouse.Api.Domain.Entities.Bases;

public class BaseRequest
{
    [JsonIgnore]
    public Guid UserId { get; set; }

    [JsonIgnore]
    public Guid CompanyId { get; set; }

    [JsonIgnore]
    public string ModuleCode { get; set; } = string.Empty;

}