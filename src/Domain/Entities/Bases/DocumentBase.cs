using ERP.Core.Database.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Domain.Entities.Bases
{
    public abstract class DocumentBase {
        public string? Concept { get; set; }

        public UserInformation RegisteredByUser { get; set; } = new ();
        public CompanyInformation CompanyInformation { get; set; } = new ();
    }

    public class CompanyInformation
    {
        public string? Ruc { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyAlias { get; set; }
        public string? CompanyLogoUrl { get; set; }
    }

    public class UserInformation
    {
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
    }
}