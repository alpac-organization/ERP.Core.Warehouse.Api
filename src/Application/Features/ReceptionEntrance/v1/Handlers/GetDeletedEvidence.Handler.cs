using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class GetDeletedEvidencesHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager)
    : BaseValidatorHandler<GetDeletedEvidencesQuery, GetDeletedEvidencesDto>(unitOfWork, errorManager)
{
    public override async Task<GetDeletedEvidencesDto> Handle(GetDeletedEvidencesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        // Query base: Recepciones con DeletedEvidenceUrls no vacío (incluyendo soft deleted)
        var query = _unitOfWork.ReceptionEntrance.Entities
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.DeletedEvidenceUrls != null && r.DeletedEvidenceUrls.Count > 0);

        // Conteo total
        var totalCount = await query.CountAsync(cancellationToken);

        // Paginación
        var data = await query
            .OrderByDescending(r => r.DeletedAt ?? r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new DeletedEvidenceListItemDto
            {
                RecordEntranceId = r.RecordEntranceId,
                DeletedEvidenceUrls = r.DeletedEvidenceUrls ?? new List<string>()
            })
            .ToListAsync(cancellationToken);

        return new GetDeletedEvidencesDto
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}