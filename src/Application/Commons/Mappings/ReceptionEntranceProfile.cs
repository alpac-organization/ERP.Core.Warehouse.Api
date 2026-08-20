using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;
using ReceptionEntranceEntity = ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public static class ReceptionEntranceMapper
{
    #region Create Endpoint
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
            CustomBranchId = dto.CustomBranchId,
            VehiclePlateNumber = dto.VehiclePlateNumber.SanitizeCode(),
            VehicleChassisNumber = dto.VehicleChassisNumber.SanitizeCode(),
            DriverLicense = dto.DriverLicense.SanitizeCode(),
            Transportista = dto.Transportista.SanitizeAlphanumeric(),
            TransportUnit = dto.TransportUnit,
            DriverName = dto.DriverName.SanitizeAlphanumeric(),
            SealNumber = dto.SealNumber.SanitizeCode(),
            EvidenceBase64 = dto.EvidenceBase64,
            StartDate = dto.StartDate,
            StartTime = dto.StartTime
        };
    }

    #endregion

    #region Update Endpoint

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
            CustomBranchId = dto.CustomBranchId,
            VehiclePlateNumber = dto.VehiclePlateNumber?.SanitizeCode(),
            VehicleChassisNumber = dto.VehicleChassisNumber?.SanitizeCode(),
            ContainerNumber = dto.ContainerNumber?.SanitizeCode(),
            DriverLicense = dto.DriverLicense?.SanitizeCode(),
            Transportista = dto.Transportista?.SanitizeAlphanumeric(),
            TransportUnit = dto.TransportUnit,
            DriverName = dto.DriverName?.SanitizeAlphanumeric(),
            SealNumber = dto.SealNumber?.SanitizeCode(),
            EvidenceToDelete = dto.EvidenceToDelete,
            EvidenceToAdd = dto.EvidenceToAdd,

            CustomsDeclarationNumber = dto.CustomsDeclarationNumber?.SanitizeCode(),
            Packages = dto.Packages,
            Customer = dto.Customer?.SanitizeAlphanumeric(),
            Product = dto.Product?.SanitizeAlphanumeric(),
        };
    }

    public static void ApplyUpdate(
        this ReceptionEntranceEntity entity,
        UpdateReceptionEntranceCommand command,
        string updatedByUserId,
        string updatedByUserName,
        DateOnly updatedDate,
        TimeOnly updatedTime)
    {
        if (command.CountryOfOrigin != null) entity.CountryOfOrigin = command.CountryOfOrigin;
        if (command.CustomBranchId != null) entity.CustomBranchId = command.CustomBranchId.Value;
        if (command.VehiclePlateNumber != null) entity.VehiclePlateNumber = command.VehiclePlateNumber;
        if (command.VehicleChassisNumber != null) entity.VehicleChassisNumber = command.VehicleChassisNumber;
        if (command.ContainerNumber != null) entity.ContainerNumber = command.ContainerNumber;
        if (command.DriverLicense != null) entity.DriverLicense = command.DriverLicense;
        if (command.Transportista != null) entity.Transportista = command.Transportista;
        if (command.TransportUnit != null) entity.TransportUnit = command.TransportUnit.Value;
        if (command.DriverName != null) entity.DriverName = command.DriverName;
        if (command.SealNumber != null) entity.SealNumber = command.SealNumber;

        entity.UpdatedByUserId = updatedByUserId;
        entity.UpdatedByUserName = updatedByUserName;
        entity.UpdatedDate = updatedDate;
        entity.UpdatedTime = updatedTime;
    }

    public static void ApplyUpdate(this CustomsDeclarations declaration, UpdateReceptionEntranceCommand command)
    {
        if (command.CustomsDeclarationNumber != null)
            declaration.CustomsDeclarationNumber = command.CustomsDeclarationNumber.Trim();
    }

    public static void ApplyUpdate(this CustomsDeclarationDetails details, UpdateReceptionEntranceCommand command)
    {
        if (command.Packages != null) details.Packages = command.Packages.Value;
        if (command.Customer != null) details.Customer = command.Customer;
        if (command.Product != null) details.Product = command.Product;
    }

    public static void ApplyUpdate(this EntranceDucats ducat, string newDucatNumber)
    {
        ducat.DucatNumber = newDucatNumber;
    }

    #endregion

    #region Add DUCA Endpoint

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

    #endregion

    #region Exit Endpoint

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
            ExitVehicle = dto?.ExitVehicle ?? false,
            ExitContainer = dto?.ExitContainer ?? false,
            ExitDate = dto?.ExitDate,
            ExitTime = dto?.ExitTime
        };
    }

    public static void ApplyVehicleExit(this ReceptionEntranceEntity entity, DateOnly exitDate, TimeOnly exitTime)
    {
        entity.VehicleExitDate = exitDate;
        entity.VehicleExitTime = exitTime;
    }

    public static void ApplyContainerExit(this ReceptionEntranceEntity entity, DateOnly exitDate, TimeOnly exitTime)
    {
        entity.ContainerExitDate = exitDate;
        entity.ContainerExitTime = exitTime;
    }

    #endregion
}

public class ReceptionEntranceProfile : Profile
{
    public ReceptionEntranceProfile()
    {
        #region Create Endpoint

        CreateMap<CreateReceptionEntranceCommand, RecordEntrance>()
            .ForMember(d => d.Id, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["RecordEntranceId"]))
            .ForMember(d => d.CurrentStepCode, o => o.MapFrom((src, dest, destMember, ctx) => (string)ctx.Items["StepCode"]))
            .ForMember(d => d.Status, o => o.MapFrom(_ => RecordEntranceStatus.Queue))
            .ForMember(d => d.ClosedAtDate, o => o.Ignore())
            .ForMember(d => d.ClosedAtTime, o => o.Ignore())
            .ForMember(d => d.IsConsolidated, o => o.MapFrom((src, dest, destMember, ctx) => (bool)ctx.Items["IsConsolidated"]));

        CreateMap<CreateReceptionEntranceCommand, ReceptionEntranceEntity>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["RecordEntranceId"]))
            .ForMember(d => d.CountryOfOrigin, o => o.MapFrom(s => s.CountryOfOrigin))
            .ForMember(d => d.CustomBranchId, o => o.MapFrom(s => s.CustomBranchId))
            .ForMember(d => d.VehiclePlateNumber, o => o.MapFrom(s => s.VehiclePlateNumber))
            .ForMember(d => d.VehicleChassisNumber, o => o.MapFrom(s => s.VehicleChassisNumber))
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s => s.ContainerNumber))
            .ForMember(d => d.DriverLicense, o => o.MapFrom(s => s.DriverLicense))
            .ForMember(d => d.Transportista, o => o.MapFrom(s => s.Transportista))
            .ForMember(d => d.TransportUnit, o => o.MapFrom(s => s.TransportUnit))
            .ForMember(d => d.DriverName, o => o.MapFrom(s => s.DriverName))
            .ForMember(d => d.SealNumber, o => o.MapFrom(s => s.SealNumber))
            .ForMember(d => d.EvidenceUrls, o => o.MapFrom((src, dest, destMember, ctx) => (List<string>)ctx.Items["EvidenceUrls"]))
            .ForMember(d => d.DeletedEvidenceUrls, o => o.Ignore())
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.DocumentType))
            .ForMember(d => d.CustomsBranches, o => o.Ignore())
            .ForMember(d => d.RecordEntrance, o => o.Ignore())
            .ForMember(d => d.VehicleExitDate, o => o.Ignore())
            .ForMember(d => d.VehicleExitTime, o => o.Ignore())
            .ForMember(d => d.ContainerExitDate, o => o.Ignore())
            .ForMember(d => d.ContainerExitTime, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserId, o => o.Ignore())
            .ForMember(d => d.UpdatedByUserName, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedTime, o => o.Ignore());

        CreateMap<CreateReceptionEntranceCommand, CustomsDeclarations>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["RecordEntranceId"]))
            .ForMember(d => d.CustomsDeclarationNumber, o => o.MapFrom(s => s.CustomsDeclarationNumber!.Trim()));

        CreateMap<CreateReceptionEntranceCommand, CustomsDeclarationDetails>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.CustomsDeclarationId, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["CustomsDeclarationId"]))
            .ForMember(d => d.Packages, o => o.MapFrom(s => s.Packages!.Value))
            .ForMember(d => d.Customer, o => o.MapFrom(s => s.Customer!.Trim()))
            .ForMember(d => d.Product, o => o.MapFrom(s => s.Product!.Trim()));

        CreateMap<CreateReceptionEntranceCommand, StepExecutionLogs>()
            .ForMember(d => d.Id, o => o.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.RecordEntranceId, o => o.MapFrom((src, dest, destMember, ctx) => (Guid)ctx.Items["RecordEntranceId"]))
            .ForMember(d => d.WorkflowStepDefinitionCode, o => o.MapFrom((src, dest, destMember, ctx) => (string)ctx.Items["StepCode"]))
            .ForMember(d => d.StartDate, o => o.MapFrom(s => s.StartDate))
            .ForMember(d => d.StartTime, o => o.MapFrom(s => s.StartTime))
            .ForMember(d => d.EndDate, o => o.MapFrom((src, dest, destMember, ctx) => (DateOnly)ctx.Items["EndDate"]))
            .ForMember(d => d.EndTime, o => o.MapFrom((src, dest, destMember, ctx) => (TimeOnly)ctx.Items["EndTime"]))
            .ForMember(d => d.ProcessedByUserId, o => o.MapFrom(s => s.UserId.ToString()))
            .ForMember(d => d.ProcessedByUserName, o => o.MapFrom((src, dest, destMember, ctx) => (string)ctx.Items["ProcessedByUserName"]));

        #endregion

        #region List Endpoint

        string receptionStepCode = null!;

        CreateMap<RecordEntrance, ReceptionEntranceListItemDto>()
            .ForMember(d => d.PlateNumber, o => o.MapFrom(s => s.ReceptionEntrance!.VehiclePlateNumber))
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s => s.ReceptionEntrance!.ContainerNumber))
            .ForMember(d => d.DriverName, o => o.MapFrom(s => s.ReceptionEntrance!.DriverName))
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.ReceptionEntrance!.DocumentType))
            .ForMember(d => d.ArrivalTime, o => o.MapFrom(s => s.ExecutionLogs
                .Where(l => l.WorkflowStepDefinitionCode == receptionStepCode)
                .Select(l => l.StartTime).FirstOrDefault()))
            .ForMember(d => d.VehicleExited, o => o.MapFrom(s =>
                s.ReceptionEntrance!.VehicleExitDate != null && s.ReceptionEntrance!.VehicleExitTime != null))
            .ForMember(d => d.ContainerExited, o => o.MapFrom(s =>
                s.ReceptionEntrance!.TransportUnit == TransportUnit.Container
                    ? (bool?)(s.ReceptionEntrance!.ContainerExitDate != null && s.ReceptionEntrance!.ContainerExitTime != null)
                    : null));

        #endregion

        #region Detail Endpoint

        CreateMap<EntranceDucats, EntranceDucatDetailItemDto>();

        CreateMap<CustomsDeclarations, CustomsDeclarationDetailDto>()
            .ForMember(d => d.CustomsDecarationNumber, o => o.MapFrom(s => s.CustomsDeclarationNumber))
            .ForMember(d => d.Packages, o => o.MapFrom(s => s.Details != null ? s.Details.Packages : (int?)null))
            .ForMember(d => d.Customer, o => o.MapFrom(s => s.Details != null ? s.Details.Customer : null))
            .ForMember(d => d.Product, o => o.MapFrom(s => s.Details != null ? s.Details.Product : null));

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

        CreateMap<RecordEntrance, ReceptionEntranceDetailDto>()
            .ForMember(d => d.CountryOfOrigin, o => o.MapFrom(s => s.ReceptionEntrance!.CountryOfOrigin))
            .ForMember(d => d.CustomBranch, o => o.MapFrom(s =>
                s.ReceptionEntrance!.CustomsBranches != null ? s.ReceptionEntrance!.CustomsBranches.Name : string.Empty))
            .ForMember(d => d.PlateNumber, o => o.MapFrom(s => s.ReceptionEntrance!.VehiclePlateNumber))
            .ForMember(d => d.TrailerChassis, o => o.MapFrom(s => s.ReceptionEntrance!.VehicleChassisNumber))
            .ForMember(d => d.ContainerNumber, o => o.MapFrom(s => s.ReceptionEntrance!.ContainerNumber))
            .ForMember(d => d.DriverLicense, o => o.MapFrom(s => s.ReceptionEntrance!.DriverLicense))
            .ForMember(d => d.Transportista, o => o.MapFrom(s => s.ReceptionEntrance!.Transportista))
            .ForMember(d => d.TransportUnit, o => o.MapFrom(s => s.ReceptionEntrance!.TransportUnit))
            .ForMember(d => d.DriverName, o => o.MapFrom(s => s.ReceptionEntrance!.DriverName))
            .ForMember(d => d.SealNumber, o => o.MapFrom(s => s.ReceptionEntrance!.SealNumber))
            .ForMember(d => d.EvidenceUrls, o => o.MapFrom(s => s.ReceptionEntrance!.EvidenceUrls))
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.ReceptionEntrance!.DocumentType))
            .ForMember(d => d.VehicleExitDate, o => o.MapFrom(s => s.ReceptionEntrance!.VehicleExitDate))
            .ForMember(d => d.VehicleExitTime, o => o.MapFrom(s => s.ReceptionEntrance!.VehicleExitTime))
            .ForMember(d => d.ContainerExitDate, o => o.MapFrom(s => s.ReceptionEntrance!.ContainerExitDate))
            .ForMember(d => d.ContainerExitTime, o => o.MapFrom(s => s.ReceptionEntrance!.ContainerExitTime))
            .ForMember(d => d.UpdatedByUserName, o => o.MapFrom(s => s.ReceptionEntrance!.UpdatedByUserName))
            .ForMember(d => d.UpdatedDate, o => o.MapFrom(s => s.ReceptionEntrance!.UpdatedDate))
            .ForMember(d => d.UpdatedTime, o => o.MapFrom(s => s.ReceptionEntrance!.UpdatedTime))
            .ForMember(d => d.Ducats, o => o.MapFrom(s =>
                s.ReceptionEntrance!.DocumentType == DocumentType.DUCA
                    ? s.EntranceDucats.Where(d => d.DeletedAt == null)
                    : null))
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

        #endregion
    }
}