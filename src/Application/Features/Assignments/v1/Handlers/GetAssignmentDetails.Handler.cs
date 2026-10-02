using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

using ERP.Core.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class GetAssignmentDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetAssignmentDetailsQuery, AssignmentOperationalDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<AssignmentOperationalDetailsDto> Handle(GetAssignmentDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return _errorManager.ThrowNotFound<AssignmentOperationalDetailsDto>("No se encontró la asignación", "ERP:NOT_FOUND");
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<AssignmentOperationalDetailsDto>("No tienes acceso a realizar esta acción", "ERP:INVALID_ACCESS");
            }

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Include(ao => ao.AssignmentsMachineries)
                    .ThenInclude(m => m.Machinery)
                .Include(ao => ao.AssignmentCollaborators)
                    .ThenInclude(c => c.Collaborator)
                .Include(ao => ao.Warehouse)
                .Include(ao => ao.OperationalOrder)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(ao => ao.Id == request.AssignmentId, cancellationToken);

            if (assignment == null)
            {
                return _errorManager.ThrowNotFound<AssignmentOperationalDetailsDto>("La asignación no existe", "ERP:NOT_FOUND_ASSIGNMENT");
            }
            
            return _mapper.Map<AssignmentOperationalDetailsDto>(assignment);
        }
    }
}