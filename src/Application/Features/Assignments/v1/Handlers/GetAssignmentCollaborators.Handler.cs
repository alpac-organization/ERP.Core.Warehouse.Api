using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class GetAssignmentCollaboratorsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetAssignmentCollaboratorsQuery, PagedResponse<GetAssignmentCollaboratorsDto>>(unitOfWork, errorManager)
{
    public override async Task<PagedResponse<GetAssignmentCollaboratorsDto>> Handle(GetAssignmentCollaboratorsQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignQuery = _unitOfWork.AssignmentCollaborators.Entities
            .Where(ac => ac.AssignmentOperationalId == request.AssignmentId)
            .Where(ac => ac.IsActive && ac.DeletedAt == null)
            .AsNoTracking();
        
        var totalRecords = await assignQuery.CountAsync(ct)

        var collaboratorName = await _unitOfWork.Collaborators.Entities
            .Where(cn => cn.Id == assignQuery.)
    }
}