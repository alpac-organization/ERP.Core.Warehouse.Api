using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

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
            UserId                      = userId,
            CompanyId                   = companyId,
            ModuleCode                  = moduleCode,

            DucatNumbers                = dto.DucatNumbers,
            CountryOfOrigin             = dto.CountryOfOrigin,
            Aduana                      = dto.Aduana,
            PlateNumber                 = dto.PlateNumber,
            TrailerChassis              = dto.TrailerChassis,
            DriverLicense               = dto.DriverLicense,
            Transportista               = dto.Transportista,
            Medio                       = dto.Medio,
            DriverName                  = dto.DriverName,
            Consignee                   = dto.Consignee,
            SealNumber                  = dto.SealNumber,
            StartDate                   = dto.StartDate,
            StartTime                   = dto.StartTime
        };
    }

    public static UpdateReceptionEntranceCommand ToUpdateCommand(
        this UpdateReceptionEntranceDto dto,
        Guid receptionId,
        Guid userId,
        Guid companyId,
        string moduleCode)
    {
        return new()
        {
            ReceptionId     = receptionId,
            UserId          = userId,
            CompanyId       = companyId,
            ModuleCode      = moduleCode,

            Ducats          = dto.Ducats,
            CountryOfOrigin = dto.CountryOfOrigin,
            Aduana          = dto.Aduana,
            PlateNumber     = dto.PlateNumber,
            TrailerChassis  = dto.TrailerChassis,
            DriverLicense   = dto.DriverLicense,
            Transportista   = dto.Transportista,
            Medio           = dto.Medio,
            DriverName      = dto.DriverName,
            Consignee       = dto.Consignee,
            SealNumber      = dto.SealNumber,
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
            Id              = recordEntranceId,
            ServiceOrderId  = null,
            CurrentStepCode = stepCode,
            Status          = RecordEntranceStatus.InTail,
            ClosedAtDate    = null,
            ClosedAtTime    = null,
            IsConsolidated  = isConsolidated
        };
    }

    public static ReceptionEntrance ToReceptionEntranceEntity(this CreateReceptionEntranceCommand command, Guid recordEntranceId)
    {
        return new()
        {
            Id                  = Guid.NewGuid(),
            RecordEntranceId    = recordEntranceId,
            CountryOfOrigin     = command.CountryOfOrigin,
            Aduana              = command.Aduana,
            PlateNumber         = command.PlateNumber,
            TrailerChassis      = command.TrailerChassis,
            DriverLicense       = command.DriverLicense,
            Transportista       = command.Transportista,
            Medio               = command.Medio,
            DriverName          = command.DriverName,
            Consignee           = command.Consignee,
            SealNumber          = command.SealNumber
        };
    }

    public static EntranceDucats ToEntranceDucaEntity(this string ducatNumber, Guid recordEntranceId)
    {
        return new()
        {
            Id                  = Guid.NewGuid(),
            DucatNumber         = ducatNumber.Trim().Replace(" ", ""),
            RecordEntranceId    = recordEntranceId
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
            ReceptionId     = receptionId,
            UserId          = userId,
            CompanyId       = companyId,
            ModuleCode      = moduleCode,
            DucatNumbers    = dto.DucatNumbers
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
            Id                          = Guid.NewGuid(),
            RecordEntranceId            = recordEntranceId,
            WorkflowStepDefinitionCode  = stepCode,
            StartDate                   = command.StartDate,
            StartTime                   = command.StartTime,
            EndDate                     = endDate,
            EndTime                     = endTime,
            ProcessedByUserId           = command.UserId.ToString(),
            ProcessedByUserName         = processedByUserame
        };
    }
}