using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1;

public static class WarehouseAssignmentRules
{
    public const string AssignmentStepCode = "ASWB";

    public static bool IsStepTwoCompleted(RecordEntrance record)
    {
        if (record.ReceptionEntrance == null) return false;

        return record.ReceptionEntrance.DocumentType switch
        {
            DocumentType.DUCA => record.EntranceDucats.Any(d => d.DeletedAt == null)
                && record.EntranceDucats.Where(d => d.DeletedAt == null)
                    .All(d => d.Status == DucaStatus.Completed),
            DocumentType.CustomsDeclaration => record.CustomsDeclarations != null
                && record.CustomsDeclarations.Details != null,
            _ => false
        };
    }

    public static WarehouseType AllowedWarehouseType(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.DUCA => WarehouseType.Fiscal,
            DocumentType.CustomsDeclaration => WarehouseType.General,
            _ => WarehouseType.General
        };
    }
}