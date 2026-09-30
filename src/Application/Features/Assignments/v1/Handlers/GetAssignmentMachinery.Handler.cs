using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class GetAssignmentMachineryHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper)
: BaseValidatorHandler<GetAssignmentMachineryQuery, PagedResponse<GetAssignmentMachineryDto>>(_unitOfWork, _errorManager)
{
    public override async Task<PagedResponse<GetAssignmentMachineryDto>> Handle(GetAssignmentMachineryQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignQuery = _unitOfWork.AssignmentsMachineries.Entities
            .Where(am => am.AssignmentOperationalId == request.AssignmentId)
            .Where(am => am.IsActive && am.DeletedAt == null)
            .AsNoTracking();

        var totalRecords = await assignQuery.CountAsync(ct);

        var assign = await assignQuery
            .OrderBy(am => am.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var assignMapped = _mapper.Map<List<GetAssignmentMachineryDto>>(assign);

        return new PagedResponse<GetAssignmentMachineryDto>(
            assignMapped,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );
    }
}