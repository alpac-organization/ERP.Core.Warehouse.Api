using ERP.Core.Database.Domain.Enums;
using ERP.Core.Manager.Api.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

public class MerchandiseRegistryListItemDto
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? ContainerNumber { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public DocumentType DocumentType { get; set; }
    public int TotalDocuments { get; set; }
    public int CompletedDocuments { get; set; }
}

public class GetMerchandiseRegistryDto
{
    public List<MerchandiseRegistryListItemDto> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class MerchandiseDucatDetailDto
{
    public Guid Id { get; set; }
    public string DucatNumber { get; set; } = string.Empty;
    public DucaStatus Status { get; set; }

    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? TotalBultos { get; set; }
    public decimal? TotalWeight { get; set; }
    public string? ProductDescription { get; set; }
    public string? Remitente { get; set; }
    public string? DestinationAreaObservation { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public TimeOnly? UpdatedTime { get; set; }
}

public class MerchandiseCustomsDeclarationDetailDto
{
    public string CustomsDeclarationNumber { get; set; } = string.Empty;
    public int? Packages { get; set; }
    public string? Customer { get; set; }
    public string? Product { get; set; }
}

public class GetMerchandiseRegistryDetailDto
{
    public Guid Id { get; set; }
    public RecordEntranceStatus Status { get; set; }

    // 2. detalles de recepcion
    public MerchandiseReceptionDetailDto Reception { get; set; } = new();
    // 1. log de registro de mercancia
    public MerchandiseRegistrationLogDto MerchandiseRegistration { get; set; } = new();


    // 3. Anidado: solo uno de los dos viene lleno
    public MerchandiseDucaRegistryDetailDto? DucaRegistry { get; set; }
    public MerchandiseCustomsDeclarationDetailDto? CustomsDeclaration { get; set; }
}

public class MerchandiseReceptionDetailDto
{
    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public Guid TransportUnitId { get; set; }
    public string? TransportUnitName { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DateOnly? TransportUnitExitDate { get; set; }
    public TimeOnly? TransportUnitExitTime { get; set; }
    public string? ContainerNumber { get; set; }

}

public class MerchandiseRegistrationLogDto
{
    public DateOnly? MerchandiseRegistrationDate { get; set; }
    public TimeOnly? MerchandiseRegistrationTime { get; set; }
    public string? MerchandiseRegisteredByUserName { get; set; }
}

public class MerchandiseDucaRegistryDetailDto
{
    public string? Empresa { get; set; } // naviera
    public string? GeneralObservations { get; set; }
    public bool? IsInTransit { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public TimeOnly? UpdatedTime { get; set; }
    public List<MerchandiseDucatDetailDto>? Ducats { get; set; }
}