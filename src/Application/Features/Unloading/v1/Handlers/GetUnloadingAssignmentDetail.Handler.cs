using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Handlers;

public class GetUnloadingAssignmentDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetUnloadingAssignmentDetailQuery, UnloadingAssignmentDetailDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<UnloadingAssignmentDetailDto> Handle(
        GetUnloadingAssignmentDetailQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var assignment = await _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Warehouse)
            .Include(a => a.MachineryAssignments)
                .ThenInclude(m => m.Machinery)
            .Include(a => a.CrewAssignments)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId && a.DeletedAt == null, cancellationToken);

        if (assignment == null)
        {
            return _errorManager.ThrowBadRequest<UnloadingAssignmentDetailDto>(
                "La asignación no fue encontrada o ya ha sido eliminada.",
                "ERP:ASSIGNMENT_NOT_FOUND");
        }

        string? keeperNameFinal = assignment.WarehouseKeeperUserId;
        if (Guid.TryParse(assignment.WarehouseKeeperUserId, out var resolvedKeeperId))
        {
            var keeper = await _unitOfWork.Collaborators.Entities
                .AsNoTracking()
                .Where(c => c.Id == resolvedKeeperId)
                .Select(c => new { c.FirstName, c.SecondName, c.FirstLastname, c.SecondLastname })
                .FirstOrDefaultAsync(cancellationToken);

            if (keeper != null)
            {
                keeperNameFinal = string.Join(" ",
                    new[] { keeper.FirstName, keeper.SecondName, keeper.FirstLastname, keeper.SecondLastname }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
            }
        }

        var crewCollaboratorIds = assignment.CrewAssignments
            .Where(c => c.DeletedAt == null && !c.IsOutsourced && c.CollaboratorId.HasValue)
            .Select(c => c.CollaboratorId!.Value)
            .ToList();

        Dictionary<Guid, string> crewNamesDict = [];
        if (crewCollaboratorIds.Count > 0)
        {
            var names = await _unitOfWork.Collaborators.Entities
                .AsNoTracking()
                .Where(c => crewCollaboratorIds.Contains(c.Id))
                .Select(c => new { c.Id, c.FirstName, c.SecondName, c.FirstLastname, c.SecondLastname })
                .ToListAsync(cancellationToken);

            crewNamesDict = names.ToDictionary(
                c => c.Id,
                c => string.Join(" ",
                    new[] { c.FirstName, c.SecondName, c.FirstLastname, c.SecondLastname }
                        .Where(x => !string.IsNullOrWhiteSpace(x))));
        }

        return _mapper.Map<UnloadingAssignmentDetailDto>(assignment, opts =>
        {
            opts.Items["WarehouseKeeperUserName"] = keeperNameFinal;
            opts.Items["CrewMemberNames"] = crewNamesDict;
        });
    }
}