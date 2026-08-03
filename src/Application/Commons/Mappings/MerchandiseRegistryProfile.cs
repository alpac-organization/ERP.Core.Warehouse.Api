using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Manager.Api.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class MerchandiseRegistryProfile : Profile
{
    public MerchandiseRegistryProfile()
    {
        string receptionStepCode = null!;

        CreateMap<RecordEntrance, MerchandiseRegistryListItemDto>()
            .ForMember(d => d.PlateNumber, o => o.MapFrom(s => s.ReceptionEntrance!.PlateNumber))
            .ForMember(d => d.DriverName, o => o.MapFrom(s => s.ReceptionEntrance!.DriverName))
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.ReceptionEntrance!.DocumentType))
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration
                    ? s.CustomsDeclarations!.Details!.ContainerNumber
                    : (s.DucatRegistry != null ? s.DucatRegistry.ContainerNumber : null)))
            .ForMember(d => d.ArrivalDate, o => o.MapFrom(s => s.ExecutionLogs
                .Where(l => l.WorkflowStepDefinitionCode == receptionStepCode)
                .Select(l => l.StartDate).First()))
            .ForMember(d => d.ArrivalTime, o => o.MapFrom(s => s.ExecutionLogs
                .Where(l => l.WorkflowStepDefinitionCode == receptionStepCode)
                .Select(l => l.StartTime).First()))
            .ForMember(d => d.TotalDocuments, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.DUCA
                    ? s.EntranceDucats.Count(x => x.DeletedAt == null)
                    : (s.CustomsDeclarations != null ? 1 : 0)))
            .ForMember(d => d.CompletedDocuments, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.DUCA
                    ? s.EntranceDucats.Count(x => x.DeletedAt == null && x.Status == DucaStatus.Completed)
                    : (s.CustomsDeclarations != null && s.CustomsDeclarations.Details != null ? 1 : 0)));

        // ==== 2. Detalle de un DUCA (item hijo) ====
        // Aprovechamos la navegación EntranceDucats -> RegistryDetail -> Product
        CreateMap<EntranceDucats, MerchandiseDucatDetailDto>()
            .ForMember(d => d.ProductId, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.ProductId : (Guid?)null))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.RegistryDetail != null && s.RegistryDetail.Product != null ? s.RegistryDetail.Product.ProductName : null))
            .ForMember(d => d.TotalBultos, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.TotalBultos : (int?)null))
            .ForMember(d => d.TotalWeight, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.TotalWeight : (decimal?)null))
            .ForMember(d => d.ProductDescription, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.ProductDescription : null))
            .ForMember(d => d.Remitente, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.Remitente : null))
            .ForMember(d => d.DestinationAreaObservation, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.DestinationAreaObservation : null))
            .ForMember(d => d.UpdatedByUserName, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.UpdatedByUserName : null))
            .ForMember(d => d.UpdatedDate, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.UpdatedDate : (DateOnly?)null))
            .ForMember(d => d.UpdatedTime, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.UpdatedTime : (TimeOnly?)null));

        // ==== 3. Bloque DUCA (empresa + lista de ducats) ====
        CreateMap<RecordEntrance, MerchandiseDucaRegistryDetailDto>()
            .ForMember(d => d.Empresa, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.Empresa : null))
            .ForMember(d => d.GeneralObservations, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.GeneralObservations : null))
            .ForMember(d => d.IsInTransit, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.IsInTransit : (bool?)null))
            .ForMember(d => d.UpdatedByUserName, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.UpdatedByUserName : null))
            .ForMember(d => d.UpdatedDate, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.UpdatedDate : (DateOnly?)null))
            .ForMember(d => d.UpdatedTime, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.UpdatedTime : (TimeOnly?)null))
            .ForMember(d => d.Ducats, o => o.MapFrom(s => s.EntranceDucats));

        // ==== 4. Bloque Declaración Aduanera ====
        CreateMap<CustomsDeclarations, MerchandiseCustomsDeclarationDetailDto>()
            .ForMember(d => d.CustomsDeclarationNumber, o => o.MapFrom(s => s.CustomsDeclarationNumber))
            .ForMember(d => d.Packages, o => o.MapFrom(s => s.Details != null ? s.Details.Packages : (int?)null))
            .ForMember(d => d.Customer, o => o.MapFrom(s => s.Details != null ? s.Details.Customer : null))
            .ForMember(d => d.Product, o => o.MapFrom(s => s.Details != null ? s.Details.Product : null));

        // ==== 5. Bloque de recepción (con ContainerNumber calculado) ====
        CreateMap<RecordEntrance, MerchandiseReceptionDetailDto>()
            .ForMember(d => d.CountryOfOrigin, o => o.MapFrom(s => s.ReceptionEntrance!.CountryOfOrigin))
            .ForMember(d => d.Aduana, o => o.MapFrom(s => s.ReceptionEntrance!.Aduana))
            .ForMember(d => d.PlateNumber, o => o.MapFrom(s => s.ReceptionEntrance!.PlateNumber))
            .ForMember(d => d.TrailerChassis, o => o.MapFrom(s => s.ReceptionEntrance!.TrailerChassis))
            .ForMember(d => d.DriverLicense, o => o.MapFrom(s => s.ReceptionEntrance!.DriverLicense))
            .ForMember(d => d.Transportista, o => o.MapFrom(s => s.ReceptionEntrance!.Transportista))
            .ForMember(d => d.TransportUnitId, o => o.MapFrom(s => s.ReceptionEntrance!.TransportUnitId))
            .ForMember(d => d.TransportUnitName, o => o.MapFrom(s => s.ReceptionEntrance!.TransportUnit != null ? s.ReceptionEntrance!.TransportUnit.Name : null))
            .ForMember(d => d.DriverName, o => o.MapFrom(s => s.ReceptionEntrance!.DriverName))
            .ForMember(d => d.SealNumber, o => o.MapFrom(s => s.ReceptionEntrance!.SealNumber))
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.ReceptionEntrance!.DocumentType))
            .ForMember(d => d.TransportUnitExitDate, o => o.MapFrom(s => s.ReceptionEntrance!.TransportUnitExitDate))
            .ForMember(d => d.TransportUnitExitTime, o => o.MapFrom(s => s.ReceptionEntrance!.TransportUnitExitTime))
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration
                    ? (s.CustomsDeclarations != null ? s.CustomsDeclarations.Details!.ContainerNumber : null)
                    : (s.DucatRegistry != null ? s.DucatRegistry.ContainerNumber : null)));

        // ==== 6. Log de registro de mercancía (RECEP / REME según tipo de documento) ====
        CreateMap<RecordEntrance, MerchandiseRegistrationLogDto>()
            .ForMember(d => d.MerchandiseRegistrationDate, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.StartDate : (DateOnly?)null))
            .ForMember(d => d.MerchandiseRegistrationTime, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.StartTime : (TimeOnly?)null))
            .ForMember(d => d.MerchandiseRegisteredByUserName, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.ProcessedByUserName : null));

        // ==== 7. DTO raíz del detalle ====
        CreateMap<RecordEntrance, GetMerchandiseRegistryDetailDto>()
            .ForMember(d => d.Reception, o => o.MapFrom(s => s))
            .ForMember(d => d.MerchandiseRegistration, o => o.MapFrom(s => s))
            .ForMember(d => d.DucaRegistry, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.DUCA ? s : null))
            .ForMember(d => d.CustomsDeclaration, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration ? s.CustomsDeclarations : null));
    }

    // Regla de negocio: código de paso donde se registra la mercancía según el tipo de documento.
    private static StepExecutionLogs? ResolveMerchandiseLog(RecordEntrance s)
    {
        var stepCode = s.ReceptionEntrance?.DocumentType == DocumentType.CustomsDeclaration
            ? MerchandiseRegistrationSteps.CustomsDeclaration
            : MerchandiseRegistrationSteps.Duca;

        return s.ExecutionLogs.FirstOrDefault(l => l.WorkflowStepDefinitionCode == stepCode);
    }
}

public class MerchandiseRegistrationSteps
{
    public const string CustomsDeclaration = "RECEP";
    public const string Duca = "REME";
}

public class DucatRegistryProfile : Profile
{
    public DucatRegistryProfile()
    {
        CreateMap<CreateDucatRegistryDto, CreateDucatRegistryCommand>();

        CreateMap<CreateDucatRegistryCommand, DucatRegistry>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom(s => s.ReceptionId))
            .ForMember(d => d.RegisteredByUserId, o => o.Ignore())
            .ForMember(d => d.RegisteredByUserName, o => o.Ignore())
            .ForMember(d => d.RegisteredStartDate, o => o.Ignore())
            .ForMember(d => d.RegisteredStartTime, o => o.Ignore())
            .ForMember(d => d.RegisteredEndDate, o => o.Ignore())
            .ForMember(d => d.RegisteredEndTime, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserId, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserName, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedTime, o => o.Ignore());
    }
}