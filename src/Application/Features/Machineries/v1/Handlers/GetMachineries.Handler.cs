using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Handlers
{
    public class GetMachineriesHandler(IUnitOfWork unitOfWork, IMapper mapper) : BaseValidatorHandler<GetMachineriesQuery, List<MachineryListDto>>
    {
        public override async Task<List<MachineryListDto>> Handle(GetMachineriesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var branch = access.Profile.BranchId;

            var machineriesForBranch = await unitOfWork.Machineries.Entities
                .Where(m => m.BranchId == branch)
                .Where(m => m.IsActive)
                .AsNoTracking();

            var machinery = await machineriesForBranch;
            
            return mapper.Map<List<MachineryListDto>>(machinery);
        }
    }
}
