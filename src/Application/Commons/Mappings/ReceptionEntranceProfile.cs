using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public static class ReceptionEntranceMapper
{
    public static CreateReceptionEntranceCommand ToCommand(
        this CreateReceptionEntranceDto dto,
        Guid userId,
        Guid companyId,
        string moduleCode)
    {
        return new()
        {
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,

            DocumentType = dto.DocumentType,
            DucatNumbers = dto.DucatNumbers.SanitizeCodeList(),

            CustomsDeclarationNumber = dto.CustomsDeclarationNumber?.SanitizeCode(),
            Packages = dto.Packages,
            Customer = dto.Customer?.SanitizeAlphanumeric(),
            Product = dto.Product?.SanitizeAlphanumeric(),
            ContainerNumber = dto.ContainerNumber?.SanitizeCode(),

            CountryOfOrigin = dto.CountryOfOrigin.SanitizeAlphanumeric(),
            Aduana = dto.Aduana.SanitizeAlphanumeric(),
            PlateNumber = dto.PlateNumber.SanitizeCode(),
            TrailerChassis = dto.TrailerChassis.SanitizeCode(),
            DriverLicense = dto.DriverLicense.SanitizeCode(),
            Transportista = dto.Transportista.SanitizeAlphanumeric(),
            TransportUnitId = dto.TransportUnitId,
            DriverName = dto.DriverName.SanitizeAlphanumeric(),
            SealNumber = dto.SealNumber.SanitizeCode(),
            StartDate = dto.StartDate,
            StartTime = dto.StartTime
        };
    }

    public static UpdateReceptionEntranceCommand ToUpdateCommand(
        this UpdateReceptionEntranceDto dto,
        Guid receptionId, Guid userId, Guid companyId, string moduleCode)
    {
        return new()
        {
            ReceptionId = receptionId,
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,

            Ducats = dto.Ducats?.Select(d => new UpdateDucatItemDto
            {
                Id = d.Id,
                DucatNumber = d.DucatNumber.SanitizeCode()
            }).ToList(),

            CountryOfOrigin = dto.CountryOfOrigin?.SanitizeAlphanumeric(),
            Aduana = dto.Aduana?.SanitizeAlphanumeric(),
            PlateNumber = dto.PlateNumber?.SanitizeCode(),
            TrailerChassis = dto.TrailerChassis?.SanitizeCode(),
            DriverLicense = dto.DriverLicense?.SanitizeCode(),
            Transportista = dto.Transportista?.SanitizeAlphanumeric(),
            TransportUnitId = dto.TransportUnitId,
            DriverName = dto.DriverName?.SanitizeAlphanumeric(),
            SealNumber = dto.SealNumber?.SanitizeCode(),

            CustomsDeclarationNumber = dto.CustomsDeclarationNumber?.SanitizeCode(),
            Packages = dto.Packages,
            Customer = dto.Customer?.SanitizeAlphanumeric(),
            Product = dto.Product?.SanitizeAlphanumeric(),
            ContainerNumber = dto.ContainerNumber?.SanitizeCode(),
        };
    }


    public static ExitVehicleCommand ToExitVehicleCommand(
        this ExitVehicleDto dto,
        Guid receptionId, Guid userId, Guid companyId, string moduleCode)
    {
        return new()
        {
            ReceptionId = receptionId,
            UserId = userId,
            CompanyId = companyId,
            ModuleCode = moduleCode,
            ExitDate = dto?.ExitDate,
            ExitTime = dto?.ExitTime
        };
    }

    public static RecordEntrance ToRecordEntranceEntity(
        this CreateReceptionEntranceCommand command,
        Guid recordEntranceId,
        bool isConsolidated,
        string stepCode)
    {
        return new()
        {
            Id = recordEntranceId,
            CurrentStepCode = stepCode,
            Status = RecordEntranceStatus.Queue,
            ClosedAtDate = null,
            ClosedAtTime = null,
            IsConsolidated = isConsolidated
        };
    }

    public static ReceptionEntrance ToReceptionEntranceEntity(this CreateReceptionEntranceCommand command, Guid recordEntranceId)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            RecordEntranceId = recordEntranceId,
            CountryOfOrigin = command.CountryOfOrigin,
            Aduana = command.Aduana,
            PlateNumber = command.PlateNumber,
            TrailerChassis = command.TrailerChassis,
            DriverLicense = command.DriverLicense,
            Transportista = command.Transportista,
            TransportUnitId = command.TransportUnitId,
            DriverName = command.DriverName,
            SealNumber = command.SealNumber,
            DocumentType = command.DocumentType
        };
    }

    public static CustomsDeclarations ToCustomsDeclarationEntity(this CreateReceptionEntranceCommand command, Guid recordEntranceId)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            RecordEntranceId = recordEntranceId,
            CustomsDeclarationNumber = command.CustomsDeclarationNumber!.Trim()
        };
    }

    public static CustomsDeclarationDetails ToCustomsDeclarationDetailsEntity(this CreateReceptionEntranceCommand command, Guid customsDeclarationId)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            CustomsDeclarationId = customsDeclarationId,
            Packages = command.Packages!.Value,
            Customer = command.Customer!.Trim(),
            Product = command.Product!.Trim(),
            ContainerNumber = command.ContainerNumber!.Trim()
        };
    }

    public static EntranceDucats ToEntranceDucaEntity(this string ducatNumber, Guid recordEntranceId)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            DucatNumber = ducatNumber.Trim().Replace(" ", ""),
            RecordEntranceId = recordEntranceId,
            Status = DucaStatus.Pending
        };
    }

    public static AddDucatsToReceptionCommand ToAddDucatsCommand(
        this AddDucatsToReceptionDto dto,
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
            DucatNumbers = dto.DucatNumbers.SanitizeCodeList()
        };
    }

    public static StepExecutionLogs ToStepExecutionLogEntity(
        this CreateReceptionEntranceCommand command,
        Guid recordEntranceId,
        DateOnly endDate,
        TimeOnly endTime,
        string stepCode,
        string processedByUserame)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            RecordEntranceId = recordEntranceId,
            WorkflowStepDefinitionCode = stepCode,
            StartDate = command.StartDate,
            StartTime = command.StartTime,
            EndDate = endDate,
            EndTime = endTime,
            ProcessedByUserId = command.UserId.ToString(),
            ProcessedByUserName = processedByUserame
        };
    }
}

public class TransportUnitProfile : Profile
{
    public TransportUnitProfile()
    {
        CreateMap<TransportUnit, TransportUnitListItemDto>();
    }
}

public class ReceptionEntranceProfile : Profile
{
    public ReceptionEntranceProfile()
    {
        // 1. Mapeo para lista paginada (usado con .ProjectTo)
        string receptionStepCode = null!;

        CreateMap<RecordEntrance, ReceptionEntranceListItemDto>()
            .ForMember(d => d.PlateNumber, o => o.MapFrom(s => s.ReceptionEntrance!.PlateNumber))
            .ForMember(d => d.DriverName, o => o.MapFrom(s => s.ReceptionEntrance!.DriverName))
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.ReceptionEntrance!.DocumentType))
            .ForMember(d => d.ArrivalTime, o => o.MapFrom(s => s.ExecutionLogs
                .Where(l => l.WorkflowStepDefinitionCode == receptionStepCode)
                .Select(l => l.StartTime).FirstOrDefault()))
            .ForMember(d => d.VehicleStatus, o => o.MapFrom(s =>
            (s.ReceptionEntrance!.TransportUnitExitDate != null && s.ReceptionEntrance!.TransportUnitExitTime != null)
            ? VehicleStatus.Exited
            : VehicleStatus.OnSite));

        // 2. Mapeos para los detalles hijos
        CreateMap<EntranceDucats, EntranceDucatDetailItemDto>();

        CreateMap<CustomsDeclarations, CustomsDeclarationDetailDto>()
            .ForMember(d => d.CustomsDecarationNumber, o => o.MapFrom(s => s.CustomsDeclarationNumber))
            .ForMember(d => d.Packages, o => o.MapFrom(s => s.Details != null ? s.Details.Packages : (int?)null))
            .ForMember(d => d.Customer, o => o.MapFrom(s => s.Details != null ? s.Details.Customer : null))
            .ForMember(d => d.Product, o => o.MapFrom(s => s.Details != null ? s.Details.Product : null))
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s => s.Details != null ? s.Details.ContainerNumber : null));

        CreateMap<StepExecutionLogs, ExecutionLogDetailDto>()
            .ForMember(d => d.DurationTotalSeconds, o => o.MapFrom(s =>
                s.EndDate.HasValue && s.EndTime.HasValue
                ? (int?)(s.EndDate.Value.ToDateTime(s.EndTime.Value) - s.StartDate.ToDateTime(s.StartTime)).TotalSeconds
                : null))
            .ForMember(d => d.DurationFormatted, o => o.MapFrom(s =>
                s.EndDate.HasValue && s.EndTime.HasValue
                ? string.Format("{0:D2}:{1:D2}:{2:D2}",
                    (int)(s.EndDate.Value.ToDateTime(s.EndTime.Value) - s.StartDate.ToDateTime(s.StartTime)).TotalHours,
                    (s.EndDate.Value.ToDateTime(s.EndTime.Value) - s.StartDate.ToDateTime(s.StartTime)).Minutes,
                    (s.EndDate.Value.ToDateTime(s.EndTime.Value) - s.StartDate.ToDateTime(s.StartTime)).Seconds)
                : null));

        // 3. Mapeo para el Detalle Completo
        CreateMap<RecordEntrance, ReceptionEntranceDetailDto>()
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
            .ForMember(d => d.UpdatedByUserName, o => o.MapFrom(s => s.ReceptionEntrance!.UpdatedByUserName))
            .ForMember(d => d.UpdatedDate, o => o.MapFrom(s => s.ReceptionEntrance!.UpdatedDate))
            .ForMember(d => d.UpdatedTime, o => o.MapFrom(s => s.ReceptionEntrance!.UpdatedTime))
            .ForMember(d => d.Ducats, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.DUCA ? s.EntranceDucats : null))
            .ForMember(d => d.CustomsDeclaration, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.CustomsDeclaration ? s.CustomsDeclarations : null))
            .ForMember(d => d.ExecutionLog, o => o.MapFrom((src, dest, destMember, context) =>
            {
                if (context.Items.TryGetValue("receptionStepCode", out var code) && code is string stepCode)
                {
                    var log = src.ExecutionLogs.FirstOrDefault(l => l.WorkflowStepDefinitionCode == stepCode);
                    return context.Mapper.Map<ExecutionLogDetailDto>(log);

                }
                return null;
            }));
    }
}