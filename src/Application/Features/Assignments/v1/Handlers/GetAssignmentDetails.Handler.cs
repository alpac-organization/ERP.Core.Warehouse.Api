using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class GetAssignmentDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetAssignmentDetailsQuery, AssignmentDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<AssignmentDetailsDto> Handle(GetAssignmentDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return _errorManager.ThrowNotFound<AssignmentDetailsDto>("No se encontró la asignación", "ERP:NOT_FOUND");
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<AssignmentDetailsDto>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Include(ao => ao.AssignmentEnclosure)
                    .ThenInclude(e => e.Warehouse)
                .Include(ao => ao.AssignmentsMachineries)
                    .ThenInclude(m => m.Machinery)
                .Include(ao => ao.AssignmentCollaborators)
                    .ThenInclude(c => c.Collaborator)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(ao => ao.Id == request.AssignmentId, cancellationToken);

            if (assignment == null)
            {
                return _errorManager.ThrowNotFound<AssignmentDetailsDto>("La asignación no existe", "ERP:NOT_FOUND_ASSIGNMENT");
            }

            return _mapper.Map<AssignmentDetailsDto>(assignment);
        }
    }
}