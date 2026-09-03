using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Constants;
using WarehouseAssignmentEntity = ERP.Core.Database.Domain.Entities.Warehouse.WarehouseAssignments;

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

        public static async Task<WarehouseAssignmentEntity?> GetActiveAssignmentAsync(
            IUnitOfWork unitOfWork, Guid receptionId, Guid? entranceDucatId, CancellationToken cancellationToken = default)
        {
            var query = unitOfWork.WarehouseAssignments.Entities
                .Where(a => a.RecordEntranceId == receptionId && a.DeletedAt == null);

            query = entranceDucatId.HasValue
                ? query.Where(a => a.EntranceDucatId == entranceDucatId.Value)
                : query.Where(a => a.EntranceDucatId == null);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }
    }
}
