using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Handlers
{
    public class GetMachineriesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<GetMachineriesQuery, PagedResponse<MachineryDto>>(unitOfWork, errorManager)
    {
        private readonly IMapper _mapper = mapper;

        public override async Task<PagedResponse<MachineryDto>> Handle(GetMachineriesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var branch = access.Profile.BranchId;

            var machineryQuery = _unitOfWork.Machineries.Entities
                .Where(m => m.BranchId == branch)
                .Where(m => m.IsActive)
                .AsNoTracking();

            var totalRecords = await machineryQuery.CountAsync(cancellationToken);

            var machinery = await machineryQuery
                .OrderBy(m => m.CreatedAt)
                .Skip((request.PageNumber -1 ) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var machineryMapped = _mapper.Map<List<MachineryDto>>(machinery);

            return new PagedResponse<MachineryDto>(
                machineryMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}