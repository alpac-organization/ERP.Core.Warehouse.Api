using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public static class ReceptionEntranceMapper     
{
    public static CreateReceptionEntrancecommand ToCommand(
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

            WarehouseId                 = dto.WarehouseId,
            ServiceOrderId              = dto.ServiceOrderId,
            WorkflowStepDefinitionId    = dto.WorkflowStepDefinitionId,
            DucatNumbers                = dto.DucatNumbers,
            CountryOfOrigin             = dto.CountryOfOrigin,
            Aduana                      = dto.Aduana,
            GateEntranceTime            = dto.GateEntranceTime,
            PlateNumber                 = dto.PlateNumber,
            TrailerChassis              = dto.TrailerChassis,
            DriverLicense               = dto.DriverLicense,
            Transportista               = dto.Transportista,
            Medio                       = dto.Medio,
            DriverName                  = dto.DriverName,
            Consignee                   = dto.Consignee,
            SealNumber                  = dto.SealNumber,
            StarTime                    = dto.StartTime
        };
    }

    public static RecordEntrance ToRecordEntranceEntity(this CreateReceptionEntrancecommand command, Guid recordEntranceId, bool isConsolidated)
    {
        return new()
        {
            Id              = recordEntranceId,
            ServiceOrderId  = command.ServiceOrderId,
            WarehouseId     = command.WarehouseId,
            CurrentStepId   = command.WorkflowStepDefinitionId,
            Status          = RecordEntranceStatus.InTail,
            ClosedAt        = null,
            IsConsolidated  = isConsolidated
        };
    }

    public static ReceptionEntrance ToReceptionEntranceEntity(this CreateReceptionEntrancecommand command, Guid recordEntranceId)
    {
        return new()
        {
            Id                  = Guid.NewGuid(),
            RecordEntranceId    = recordEntranceId,
            CountryOfOrigin     = command.CountryOfOrigin,
            Aduana              = command.Aduana,
            GateEntranceTime    = command.GateEntranceTime,
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
            DucatNumber         = ducatNumber.Trim(),
            RecordEntranceId    = recordEntranceId
        };
    }

    public static StepExecutionLogs ToStepExecutionLogEntity(this CreateReceptionEntrancecommand command, Guid recordEntranceId, DateTime endTime)
    {
        return new()
        {
            Id                          = Guid.NewGuid(),
            RecordEntranceId            = recordEntranceId,
            WorkflowStepDefinitionId    = command.WorkflowStepDefinitionId,
            StartTime                   = command.StarTime,
            EndTime                     = endTime,
            ProcessedByUserId           = command.UserId.ToString()
        };
    }
}