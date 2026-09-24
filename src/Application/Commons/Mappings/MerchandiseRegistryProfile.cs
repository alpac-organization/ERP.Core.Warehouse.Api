using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class MerchandiseRegistryProfile : Profile
{
    public MerchandiseRegistryProfile()
    {

        
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
    public const string CustomsDeclaration = "REME";
    public const string Duca = "REME";
}

public class DucatRegistryProfile : Profile
{
    public DucatRegistryProfile()
    {
        CreateMap<CreateDucatRegistryCommand, DucatRegistry>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom(s => s.ReceptionId))
            .ForMember(d => d.ShippingCompanyId, o => o.MapFrom(s => s.ShippingCompanyId))
            .ForMember(d => d.GeneralObservations, o => o.MapFrom(s => s.GeneralObservations))
            .ForMember(d => d.IsInTransit, o => o.MapFrom(s => s.IsInTransit))
            .ForMember(d => d.RegisteredByUserId, o => o.Ignore())
            .ForMember(d => d.RegisteredByUserName, o => o.Ignore())
            .ForMember(d => d.RegisteredStartDate, o => o.Ignore())
            .ForMember(d => d.RegisteredStartTime, o => o.Ignore())
            .ForMember(d => d.RegisteredEndDate, o => o.Ignore())
            .ForMember(d => d.RegisteredEndTime, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserId, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserName, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedTime, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.Details, o => o.Ignore())
            .ForMember(d => d.ShippingCompany, o => o.Ignore());

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
            ShippingCompanyId = dto.ShippingCompanyId,
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
            .ForMember(d => d.DucatRegistryId, o => o.Ignore())
            .ForMember(d => d.EntranceDucatId, o => o.Ignore())
            .ForMember(d => d.MerchandiseId, o => o.MapFrom(s => s.MerchandiseId))
            .ForMember(d => d.MerchandiseName, o => o.Ignore())
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.TotalBultos, o => o.MapFrom(s => s.TotalBultos))
            .ForMember(d => d.TotalWeight, o => o.MapFrom(s => s.TotalWeight))
            .ForMember(d => d.MerchandiseDescription, o => o.MapFrom(s => s.MerchandiseDescription))
            .ForMember(d => d.Sender, o => o.MapFrom(s => s.Sender))
            .ForMember(d => d.DestinationAreaObservation, o => o.MapFrom(s => s.DestinationAreaObservation))

            .ForMember(d => d.RegisteredByUserId, o => o.Ignore())
            .ForMember(d => d.RegisteredByUserName, o => o.Ignore())
            .ForMember(d => d.RegisteredStartDate, o => o.Ignore())
            .ForMember(d => d.RegisteredStartTime, o => o.Ignore())
            .ForMember(d => d.RegisteredEndDate, o => o.Ignore())
            .ForMember(d => d.RegisteredEndTime, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserId, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserName, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedTime, o => o.Ignore())
            .ForMember(d => d.DucatRegistry, o => o.Ignore())
            .ForMember(d => d.Merchandise, o => o.Ignore());
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
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,
            MerchandiseId = dto.MerchandiseId,
            Type = dto.Type,
            TotalBultos = dto.TotalBultos,
            TotalWeight = dto.TotalWeight,
            MerchandiseDescription = dto.MerchandiseDescription.SanitizeAlphanumeric(),
            Sender = dto.Sender.SanitizeAlphanumeric(),
            DestinationAreaObservation = dto.DestinationAreaObservation.SanitizeAlphanumeric(),
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