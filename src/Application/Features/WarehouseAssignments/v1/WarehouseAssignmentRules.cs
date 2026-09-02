using System;
using System.Linq;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1
{
    public static class WarehouseAssignmentRules
    {
        public const string AssignmentStepCode = WorkflowStepCodes.Assignment;

        public static bool IsStepTwoCompleted(RecordEntrance record, Guid? entranceDucatId = null)
        {
            if (record.ReceptionEntrance == null) return false;

            if (record.ReceptionEntrance.DocumentType == DocumentType.DUCA)
            {
                if (entranceDucatId.HasValue)
                {
                    var specificDuca = record.EntranceDucats.FirstOrDefault(d => d.Id == entranceDucatId.Value && d.DeletedAt == null);
                    return specificDuca != null && specificDuca.Status == DucaStatus.Completed;
                }

                return record.EntranceDucats.Any(d => d.DeletedAt == null)
                    && record.EntranceDucats.Where(d => d.DeletedAt == null).All(d => d.Status == DucaStatus.Completed);
            }
            else if (record.ReceptionEntrance.DocumentType == DocumentType.CustomsDeclaration)
            {
                return record.CustomsDeclarations != null && record.CustomsDeclarations.Details != null;
            }

            return false;
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
}

