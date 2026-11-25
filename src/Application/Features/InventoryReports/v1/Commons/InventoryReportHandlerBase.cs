using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commons;

public abstract class InventoryReportHandlerBase<TQuery, TResponse>(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<TQuery, TResponse>(unitOfWork, errorManager)
    where TQuery : IRequest<TResponse>
{
    protected Task<string?> GetGeneratedByAsync(Guid userId, CancellationToken ct) =>
        _unitOfWork.Users.Entities
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Fullname)
            .FirstOrDefaultAsync(ct);
}
