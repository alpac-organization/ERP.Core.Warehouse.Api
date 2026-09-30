using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class GetAssignmentCollaboratorsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper)
    : BaseValidatorHandler<GetAssignmentCollaboratorsQuery, PagedResponse<GetAssignmentCollaboratorsDto>>(_unitOfWork, _errorManager)
{
    public override async Task<PagedResponse<GetAssignmentCollaboratorsDto>> Handle(GetAssignmentCollaboratorsQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignmentOperational = await _unitOfWork.AssignmentOperationals.Entities
            .Include(ao => ao.OperationalOrder)
            .AsNoTracking()
            .FirstOrDefaultAsync(ao => ao.Id == request.AssignmentId, ct);

        if (assignmentOperational is null)
            return _errorManager.ThrowBadRequest<PagedResponse<GetAssignmentCollaboratorsDto>>(
                "La asignacion operativa seleccionada no existe", "ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND");

        if (assignmentOperational.OperationalOrderId != request.OperationalOrderId)
            return _errorManager.ThrowBadRequest<PagedResponse<GetAssignmentCollaboratorsDto>>(
                "La asignacion operativa no pertenece a la orden operativa indicada", "ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH");

        if (assignmentOperational.OperationalOrder.CompanyId != request.CompanyId)
            return _errorManager.ThrowForbidden<PagedResponse<GetAssignmentCollaboratorsDto>>(
                "No tienes acceso a la asignacion operativa seleccionada", "ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH");

        var assignQuery = _unitOfWork.AssignmentCollaborators.Entities
            .Include(ac => ac.Collaborator)
            .Include(ac => ac.User)
            .Where(ac => ac.AssignmentOperationalId == request.AssignmentId)
            .Where(ac => ac.IsActive && ac.DeletedAt == null)
            .AsNoTracking();

        var totalRecords = await assignQuery.CountAsync(ct);

        var assign = await assignQuery
            .OrderBy(ac => ac.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var assignMapped = _mapper.Map<List<GetAssignmentCollaboratorsDto>>(assign);

        return new PagedResponse<GetAssignmentCollaboratorsDto>(
            assignMapped,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );
    }
}
