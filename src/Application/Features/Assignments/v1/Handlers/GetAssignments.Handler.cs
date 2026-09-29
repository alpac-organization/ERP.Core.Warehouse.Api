using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class GetAssignmentsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetAssignmentsQuery, PagedResponse<AssignmentDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<AssignmentDto>> Handle(GetAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<PagedResponse<AssignmentDto>>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var assignmentsQuery = _unitOfWork.AssignmentOperationals.Entities
                .Include(ao => ao.OperationalOrder)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(ao => ao.OperationalOrderId == request.OperationalOrderId);

            if (request.Status.HasValue)
            {
                assignmentsQuery = assignmentsQuery.Where(ao => ao.Status == request.Status);
            }

            var totalRecords = await assignmentsQuery.CountAsync(cancellationToken);

            var assignments = await assignmentsQuery
                .OrderByDescending(ao => ao.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var assignmentsMapped = _mapper.Map<List<AssignmentDto>>(assignments);

            return new PagedResponse<AssignmentDto>(
                assignmentsMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}