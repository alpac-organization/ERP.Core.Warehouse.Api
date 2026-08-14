using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;
using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class MerchandiseRegistryProfile : Profile
{
    public MerchandiseRegistryProfile()
    {
        string receptionStepCode = null!;

        // ==== 1. Lista de registros ====
        CreateMap<RecordEntrance, MerchandiseRegistryListItemDto>()
            .ForMember(d => d.PlateNumber, o => o.MapFrom(s => s.ReceptionEntrance!.PlateNumber))
            .ForMember(d => d.DriverName, o => o.MapFrom(s => s.ReceptionEntrance!.DriverName))
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.ReceptionEntrance!.DocumentType))
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration
                    ? s.CustomsDeclarations!.Details!.ContainerNumber
                    : (s.DucatRegistry != null ? s.DucatRegistry.ContainerNumber : null)))
                    ? s.CustomsDeclarations!.Details!.ContainerNumber
            : (s.DucatRegistry != null ? s.DucatRegistry.ContainerNumber : null)))
l            .ForMember(d => d.ArrivalDate, o => o.MapFrom(s => s.ExecutionLogs
                .Where(l => l.WorkflowStepDefinitionCode == receptionStepCode)
                .Select(l => l.StartDate).First()))
            .ForMember(d => d.ArrivalTime, o => o.MapFrom(s => s.ExecutionLogs
                .Where(l => l.WorkflowStepDefinitionCode == receptionStepCode)
                .Select(l => l.StartTime).First()))
            .ForMember(d => d.Duca, o => o.MapFrom(s => s.ExecutionLogs
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
        CreateMap<EntranceDucats, MerchandiseDucatDetailDto>()
            .ForMember(d => d.MerchandiseId, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.MerchandiseId : (Guid?)null))
            .ForMember(d => d.MerchandiseName, o => o.MapFrom(s => s.RegistryDetail != null && s.RegistryDetail.Merchandise != null ? s.RegistryDetail.Merchandise.MerchandiseName : null))
            .ForMember(d => d.TotalBultos, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.TotalBultos : (int?)null))
            .ForMember(d => d.TotalWeight, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.TotalWeight : (decimal?)null))
            .ForMember(d => d.ProductDescription, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.ProductDescription : null))
            .ForMember(d => d.Remitente, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.Remitente : null))
            .ForMember(d => d.DestinationAreaObservation, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.DestinationAreaObservation : null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))

            // Datos de creación / registro
            .ForMember(d => d.RegisteredByUserName, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.RegisteredByUserName : null))
            .ForMember(d => d.RegisteredStartDate, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.RegisteredStartDate : (DateOnly?)null))
            .ForMember(d => d.RegisteredStartTime, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.RegisteredStartTime : (TimeOnly?)null))
            .ForMember(d => d.RegisteredEndDate, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.RegisteredEndDate : (DateOnly?)null))
            .ForMember(d => d.RegisteredEndTime, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.RegisteredEndTime : (TimeOnly?)null))

            // Cálculos directos de duración usando Start y End
            .ForMember(d => d.DurationInSeconds, o => o.MapFrom(s => ComputeEachDucaDurationSeconds(s)))
            .ForMember(d => d.DurationFormatted, o => o.MapFrom(s => ComputeEachDucaDurationFormatted(s)))

            // Orden de servicio
            .ForMember(d => d.ServiceOrderId, o => o.MapFrom(s => s.ServiceOrderId))
            .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => s.ServiceOrderCode))

            // Datos de auditoría de actualización
            .ForMember(d => d.UpdatedByUserName, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.UpdatedByUserName : null))
            .ForMember(d => d.UpdatedDate, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.UpdatedDate : (DateOnly?)null))
            .ForMember(d => d.UpdatedTime, o => o.MapFrom(s => s.RegistryDetail != null ? s.RegistryDetail.UpdatedTime : (TimeOnly?)null));

        // ==== 3. Bloque DUCA (Dato General + lista de ducats) ====
        CreateMap<RecordEntrance, MerchandiseDucaRegistryDetailDto>()
            .ForMember(d => d.Empresa, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.Empresa : null))
            .ForMember(d => d.GeneralObservations, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.GeneralObservations : null))
            .ForMember(d => d.IsInTransit, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.IsInTransit : (bool?)null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.DucatRegistry != null ? s. : DucaStatus.Pending))

            // Datos de creación / registro
            .ForMember(d => d.RegisteredByUserName, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.RegisteredByUserName : null))
            .ForMember(d => d.RegisteredStartDate, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.RegisteredStartDate : (DateOnly?)null))
            .ForMember(d => d.RegisteredStartTime, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.RegisteredStartTime : (TimeOnly?)null))
            .ForMember(d => d.RegisteredEndDate, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.RegisteredEndDate : (DateOnly?)null))
            .ForMember(d => d.RegisteredEndTime, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.RegisteredEndTime : (TimeOnly?)null))

            // Cálculos directos de duración usando Start y End del Dato General
            .ForMember(d => d.DurationInSeconds, o => o.MapFrom(s => ComputeDatoGeneralDurationSeconds(s)))
            .ForMember(d => d.DurationFormatted, o => o.MapFrom(s => ComputeDatoGeneralDurationFormatted(s)))

            // Datos de auditoría de actualización
            .ForMember(d => d.UpdatedByUserName, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.UpdatedByUserName : null))
            .ForMember(d => d.UpdatedDate, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.UpdatedDate : (DateOnly?)null))
            .ForMember(d => d.UpdatedTime, o => o.MapFrom(s => s.DucatRegistry != null ? s.DucatRegistry.UpdatedTime : (TimeOnly?)null))

            .ForMember(d => d.Ducats, o => o.MapFrom(s => s.EntranceDucats));

        // ==== 4. Bloque Declaración Aduanera ====
        CreateMap<CustomsDeclarations, MerchandiseCustomsDeclarationDetailDto>()
            .ForMember(d => d.CustomsDeclarationNumber, o => o.MapFrom(s => s.CustomsDeclarationNumber))
            .ForMember(d => d.Packages, o => o.MapFrom(s => s.Details != null ? s.Details.Packages : (int?)null))
            .ForMember(d => d.Customer, o => o.MapFrom(s => s.Details != null ? s.Details.Customer : null))
            .ForMember(d => d.Product, o => o.MapFrom(s => s.Details != null ? s.Details.Product : null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
            .ForMember(d => d.ServiceOrderId, o => o.MapFrom(s => s.ServiceOrderId))
            .ForMember(d => d.ServiceOrderCode, o => o.MapFrom(s => s.ServiceOrderCode));

        // ==== 5. Bloque de recepción ====
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
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s => ResolveContainerNumber(s)));

        // ==== 6. Log de registro de mercancía (Workflow Step Execution Log) ====
        CreateMap<RecordEntrance, MerchandiseRegistrationLogDto>()
            .ForMember(d => d.MerchandiseRegistrationDate, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.StartDate : (DateOnly?)null))
            .ForMember(d => d.MerchandiseRegistrationTime, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.StartTime : (TimeOnly?)null))
            .ForMember(d => d.MerchandiseRegisteredByUserName, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.ProcessedByUserName : null))
            .ForMember(d => d.MerchandiseRegistrationEndDate, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.EndDate : null))
            .ForMember(d => d.MerchandiseRegistrationEndTime, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.EndTime : null))
            .ForMember(d => d.MerchandiseFinishedByUserName, o => o.MapFrom(s => ResolveMerchandiseLog(s) != null ? ResolveMerchandiseLog(s)!.FinishedByUserName : null))
            .ForMember(d => d.DurationTotalSeconds, o => o.MapFrom(s => ComputeDurationSeconds(ResolveMerchandiseLog(s))))
            .ForMember(d => d.DurationFormatted, o => o.MapFrom(s => ComputeDurationFormatted(ResolveMerchandiseLog(s))));

        // ==== 7. DTO raíz del detalle ====
        CreateMap<RecordEntrance, GetMerchandiseRegistryDetailDto>()
            .ForMember(d => d.Reception, o => o.MapFrom(s => s))
            .ForMember(d => d.MerchandiseRegistration, o => o.MapFrom(s => s))
            .ForMember(d => d.DucaRegistry, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.DUCA ? s : null))
            .ForMember(d => d.CustomsDeclaration, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration ? s.CustomsDeclarations : null));
    }

    // ==== RESOLUCIÓN DE CAMPOS COMPARTIDOS (evita duplicar el ternario en varios bloques) ====

    private static string? ResolveContainerNumber(RecordEntrance s)
    {
        return s.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration
            ? s.CustomsDeclarations?.Details?.ContainerNumber
            : s.DucatRegistry?.ContainerNumber;
    }

    private static string ResolveReceptionStepCode(RecordEntrance s)
    {
        return s.ReceptionEntrance?.DocumentType == DocumentType.CustomsDeclaration
            ? MerchandiseRegistrationSteps.CustomsDeclaration
            : MerchandiseRegistrationSteps.Duca;
    }

    private static StepExecutionLogs? ResolveMerchandiseLog(RecordEntrance s)
    {
        var stepCode = ResolveReceptionStepCode(s);
        return s.ExecutionLogs.FirstOrDefault(l => l.WorkflowStepDefinitionCode == stepCode);
    }

    // ⚠️ PENDIENTE DE CONFIRMAR: antes, "receptionStepCode" nunca se asignaba (bug),
    // por lo que este filtro nunca encontraba coincidencias. Necesito saber cuál es
    // el código real del paso de "llegada del vehículo" para reemplazar el placeholder.
    private static StepExecutionLogs? ResolveArrivalLog(RecordEntrance s)
    {
        var stepCode = ResolveReceptionStepCode(s); // TODO: confirmar si es el mismo paso que MerchandiseRegistration o uno distinto
        return s.ExecutionLogs.FirstOrDefault(l => l.WorkflowStepDefinitionCode == stepCode);
    }

    // ==== MÉTODOS DE CÁLCULO DE DURACIÓN ====

    private static int? ComputeDatoGeneralDurationSeconds(RecordEntrance s)
    {
        if (s.DucatRegistry == null) return null;
        return ComputeDurationSeconds(
            s.DucatRegistry.RegisteredStartDate,
            s.DucatRegistry.RegisteredStartTime,
            s.DucatRegistry.RegisteredEndDate,
            s.DucatRegistry.RegisteredEndTime);
    }

    private static string? ComputeDatoGeneralDurationFormatted(RecordEntrance s)
    {
        if (s.DucatRegistry == null) return null;
        return ComputeDurationFormatted(
            s.DucatRegistry.RegisteredStartDate,
            s.DucatRegistry.RegisteredStartTime,
            s.DucatRegistry.RegisteredEndDate,
            s.DucatRegistry.RegisteredEndTime);
    }

    private static int? ComputeEachDucaDurationSeconds(EntranceDucats current)
    {
        if (current.RegistryDetail == null) return null;
        return ComputeDurationSeconds(
            current.RegistryDetail.RegisteredStartDate,
            current.RegistryDetail.RegisteredStartTime,
            current.RegistryDetail.RegisteredEndDate,
            current.RegistryDetail.RegisteredEndTime);
    }

    private static string? ComputeEachDucaDurationFormatted(EntranceDucats current)
    {
        if (current.RegistryDetail == null) return null;
        return ComputeDurationFormatted(
            current.RegistryDetail.RegisteredStartDate,
            current.RegistryDetail.RegisteredStartTime,
            current.RegistryDetail.RegisteredEndDate,
            current.RegistryDetail.RegisteredEndTime);
    }

    // ==== SOBRECARGAS PARA StepExecutionLogs ====
    private static int? ComputeDurationSeconds(StepExecutionLogs? log)
    {
        if (log == null) return null;
        return ComputeDurationSeconds(log.StartDate, log.StartTime, log.EndDate, log.EndTime);
    }

    private static string? ComputeDurationFormatted(StepExecutionLogs? log)
    {
        if (log == null) return null;
        return ComputeDurationFormatted(log.StartDate, log.StartTime, log.EndDate, log.EndTime);
    }

    // ==== MÉTODO BASE PARA CÁLCULO DE DIFERENCIA DE TIEMPO ====
    private static int? ComputeDurationSeconds(DateOnly? startDate, TimeOnly? startTime, DateOnly? endDate, TimeOnly? endTime)
    {
        if (!startDate.HasValue || !startTime.HasValue || !endDate.HasValue || !endTime.HasValue) return null;
        return (int)(endDate.Value.ToDateTime(endTime.Value) - startDate.Value.ToDateTime(startTime.Value)).TotalSeconds;
    }

    private static string? ComputeDurationFormatted(DateOnly? startDate, TimeOnly? startTime, DateOnly? endDate, TimeOnly? endTime)
    {
        if (!startDate.HasValue || !startTime.HasValue || !endDate.HasValue || !endTime.HasValue) return null;
        var span = endDate.Value.ToDateTime(endTime.Value) - startDate.Value.ToDateTime(startTime.Value);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)span.TotalHours, span.Minutes, span.Seconds);
    }
}

#region Crear Registro
public class MerchandiseRegistrationSteps
{
    public const string CustomsDeclaration = "RECEP";
    public const string Duca = "REME";
}

public class DucatRegistryProfile : Profile
{
    public DucatRegistryProfile()
    {
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

public static class DucatRegistryMapper
{
    public static CreateDucatRegistryCommand ToCommand(
        this CreateDucatRegistryDto dto,
        Guid receptionId,
        Guid userId,
        Guid companyId,
        string moduleCode)
    {
        return new()
        {
            ReceptionId = receptionId,
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,
            ContainerNumber = dto.ContainerNumber,
            Empresa = dto.Empresa,
            GeneralObservations = dto.GeneralObservations,
            IsInTransit = dto.IsInTransit,
            RegisteredStartDate = dto.RegisteredStartDate,
            RegisteredStartTime = dto.RegisteredStartTime
        };
    }
}

#endregion

#region  Crear registro detalle
public class DucatRegistryDetailProfile : Profile
{
    public DucatRegistryDetailProfile()
    {
        CreateMap<CreateDucatRegistryDetailCommand, DucatRegistryDetails>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.RecordEntranceId, o => o.Ignore())
            .ForMember(d => d.EntranceDucatId, o => o.Ignore())
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

public static class DucatRegistryDetailMapper
{
    public static CreateDucatRegistryDetailCommand ToCommand(
        this CreateDucatRegistryDetailDto dto,
        Guid receptionId,
        Guid entranceDucatId,
        Guid userId,
        Guid companyId,
        string moduleCode)
    {
        return new()
        {
            ReceptionId = receptionId,
            EntranceDucatId = entranceDucatId,
            ServiceOrderId = dto.ServiceOrderId,
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,
            MerchandiseId = dto.MerchandiseId,
            TotalBultos = dto.TotalBultos,
            TotalWeight = dto.TotalWeight,
            ProductDescription = dto.ProductDescription,
            Remitente = dto.Remitente,
            DestinationAreaObservation = dto.DestinationAreaObservation,
            RegisteredStartDate = dto.RegisteredStartDate,
            RegisteredStartTime = dto.RegisteredStartTime
        };
    }
}
#endregion

public class MerchandiseProfile : Profile
{
    public MerchandiseProfile()
    {
        CreateMap<Merchandises, MerchandiseDto>()
           .ForMember(dest => dest.MerchandiseId, opt => opt.MapFrom(src => src.Id));

        CreateMap<Merchandises, MerchandiseDucatDetailDto>()
            .ForMember(dest => dest.MerchandiseId, opt => opt.MapFrom(src => src.Id));

        CreateMap<CategoryProducts, MerchandiseCategoryDto>();

    }
}
public static class RegisterMerchandiseMapper
{
    public static RegisterMerchandiseCommand ToCommand(
        this RegisterMerchandiseDto dto,
        Guid userId,
        Guid companyId,
        string moduleCode)
    {
        return new()
        {
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,
            MerchandiseName = dto.MerchandiseName,
            Description = dto.Description,
            CategoryId = dto.CategoryId
        };
    }

}