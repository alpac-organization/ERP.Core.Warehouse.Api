using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Handlers
{
    public class GetMachineriesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<GetMachineriesQuery, IEnumerable<MachineryDto>>(unitOfWork, errorManager)
    {
        private readonly IMapper _mapper = mapper;

        public override async Task<IEnumerable<MachineryDto>> Handle(GetMachineriesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var branch = access.Profile.BranchId;

            var machinery = await _unitOfWork.Machineries.Entities
                .Where(m => m.BranchId == branch)
                .Where(m => m.IsActive)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<MachineryDto>>(machinery);
        }
    }
}