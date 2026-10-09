using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Operations;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public static class AssignmentOperationalFinder
    {
        public static Task<AssignmentOperational?> FindActiveByOrderAsync(
            IUnitOfWork unitOfWork, Guid assignmentId, Guid operationalOrderId, CancellationToken ct)
            => unitOfWork.AssignmentOperationals.Entities
                .Where(a => a.Id == assignmentId && a.OperationalOrderId == operationalOrderId)
                .Where(a => a.IsActive && a.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

        public static Task<AssignmentOperational?> FindActiveWithStockPlacementsAsync(
            IUnitOfWork unitOfWork, Guid assignmentId, Guid operationalOrderId, CancellationToken ct)
            => unitOfWork.AssignmentOperationals.Entities
                .Where(a => a.Id == assignmentId && a.OperationalOrderId == operationalOrderId)
                .Where(a => a.IsActive && a.DeletedAt == null)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(p => p.LotPosition)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(p => p.RackPosition)
                .Include(a => a.OperationalOrder)
                .FirstOrDefaultAsync(ct);
    }
}