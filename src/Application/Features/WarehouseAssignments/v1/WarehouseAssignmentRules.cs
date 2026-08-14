using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1;

public static class WarehouseAssignmentRules
{
    public const string AssignmentStepCode = "ASWB";

    public static bool IsDocumentStepTwoCompleted(EntranceDucats ducat)
    {
        return ducat.Status == DucaStatus.Completed;
    }

    public static bool IsDocumentStepTwoCompleted(CustomsDeclarations declaration)
    {
        return declaration.Status == DucaStatus.Completed;
    }

    /// <summary>
    /// Las declaraciones aduaneras solo pueden ir a bodegas tipo General.
    /// Las ducas pueden ir a cualquier tipo de bodega excepto General y Granel.
    /// </summary>
    public static List<WarehouseType> AllowedWarehouseTypes(DocumentType documentType)
    {
        return documentType switch
        {
            DocumentType.CustomsDeclaration => [WarehouseType.General],
            DocumentType.DUCA => [WarehouseType.Fiscal, WarehouseType.GaleronTechado, WarehouseType.PatioContenedores, WarehouseType.PredioAbierto],
            _ => []
        };
    }

    public static bool IsWarehouseTypeAllowed(WarehouseType warehouseType, DocumentType documentType)
    {
        return AllowedWarehouseTypes(documentType).Contains(warehouseType);
    }
}
