using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Handlers;

public class GetCustomBranchesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetCustomBranchesQuery, List<CustomBranchListItemDto>>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<List<CustomBranchListItemDto>> Handle(GetCustomBranchesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var customsBranches = await _unitOfWork.CustomsBranches.Entities
            .AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderBy(c => c.Name)
            .ProjectTo<CustomBranchListItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return customsBranches;
    }
}