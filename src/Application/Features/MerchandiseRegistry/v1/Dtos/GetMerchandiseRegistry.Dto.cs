using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;

public class MerchandiseRegistryListItemDto
{
    public Guid Id { get; set; }
    public string VehiclePlateNumber { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? ContainerNumber { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public TimeOnly ArrivalTime { get; set; }
    public DocumentType DocumentType { get; set; }
    public int TotalDocuments { get; set; }
    public int CompletedDocuments { get; set; }
    public DucaStatus? Status { get; set; }
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
    public DucaType Type { get; set; }

    public Guid? MerchandiseId { get; set; }
    public string? MerchandiseName { get; set; }
    public int? TotalBultos { get; set; }
    public decimal? TotalWeight { get; set; }
    public string? MerchandiseDescription { get; set; }
    public string? Sender { get; set; }
    public string? DestinationAreaObservation { get; set; }

    public Guid? ServiceOrderId { get; set; }
    public string? ServiceOrderCode { get; set; }

    public string? RegisteredByUserName { get; set; }
    public DateOnly? RegisteredStartDate { get; set; }
    public DateOnly? RegisteredEndDate { get; set; }
    public TimeOnly? RegisteredStartTime { get; set; }
    public TimeOnly? RegisteredEndTime { get; set; }
    public int? DurationInSeconds { get; set; }
    public string? DurationFormatted { get; set; }

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

    public Guid? ServiceOrderId { get; set; }
    public string? ServiceOrderCode { get; set; }
    public DucaStatus Status { get; set; }
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
    public string CustomBranch { get; set; } = string.Empty;
    public string VehiclePlateNumber { get; set; } = string.Empty;
    public string VehicleChassisNumber { get; set; } = string.Empty;
    public string? ContainerNumber { get; set; }
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;
    public string SealEvidence { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public TransportUnit TransportUnit { get; set; }
    public DateOnly? VehicleExitDate { get; set; }
    public TimeOnly? VehicleExitTime { get; set; }
    public DateOnly? ContainerExitDate { get; set; }
    public TimeOnly? ContainerExitTime { get; set; }
}

public class MerchandiseRegistrationLogDto
{
    public DateOnly? MerchandiseRegistrationEndDate { get; set; }
    public TimeOnly? MerchandiseRegistrationEndTime { get; set; }
    public string? MerchandiseFinishedByUserName { get; set; }
    public int? DurationTotalSeconds { get; set; }
    public string? DurationFormatted { get; set; }
    public DateOnly? MerchandiseRegistrationDate { get; set; }
    public TimeOnly? MerchandiseRegistrationTime { get; set; }
    public string? MerchandiseRegisteredByUserName { get; set; }
}

public class MerchandiseDucaRegistryDetailDto
{
    public Guid? ShippingCompanyId { get; set; } // naviera
    public string? SippingCompanyName { get; set; }
    public string? GeneralObservations { get; set; }
    public bool? IsInTransit { get; set; }

    public DucaStatus Status { get; set; }

    public string? RegisteredByUserName { get; set; }
    public DateOnly? RegisteredStartDate { get; set; }
    public DateOnly? RegisteredEndDate { get; set; }
    public TimeOnly? RegisteredStartTime { get; set; }
    public TimeOnly? RegisteredEndTime { get; set; }

    public string? UpdatedByUserName { get; set; }
    public DateOnly? UpdatedDate { get; set; }
    public TimeOnly? UpdatedTime { get; set; }

    public int? DurationInSeconds { get; set; }
    public string? DurationFormatted { get; set; }
    public List<MerchandiseDucatDetailDto>? Ducats { get; set; }
}