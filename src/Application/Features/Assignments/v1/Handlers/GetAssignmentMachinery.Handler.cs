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

        var assignmentOperational = await _unitOfWork.AssignmentOperationals.Entities
            .Include(ao => ao.OperationalOrder)
            .AsNoTracking()
            .FirstOrDefaultAsync(ao => ao.Id == request.AssignmentId, ct);

        if (assignmentOperational is null)
            return _errorManager.ThrowBadRequest<PagedResponse<GetAssignmentMachineryDto>>(
                "La asignacion operativa seleccionada no existe", "ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND");

        if (assignmentOperational.OperationalOrderId != request.OperationalOrderId)
            return _errorManager.ThrowBadRequest<PagedResponse<GetAssignmentMachineryDto>>(
                "La asignacion operativa no pertenece a la orden operativa indicada", "ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH");

        if (assignmentOperational.OperationalOrder.CompanyId != request.CompanyId)
            return _errorManager.ThrowForbidden<PagedResponse<GetAssignmentMachineryDto>>(
                "No tienes acceso a la asignacion operativa seleccionada", "ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH");

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