using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers
{
    public class GetPhysicalCountsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
        : BaseValidatorHandler<GetPhysicalCountsQuery, List<PhysicalCountListItemDto>>(unitOfWork, errorManager)
    {
        public override async Task<List<PhysicalCountListItemDto>> Handle(GetPhysicalCountsQuery request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
            if (!access.IsSuccess)
                return access.ErrorResponse!;

            var physicalCounts = await _unitOfWork.PhysicalCounts.Entities
                .AsNoTracking()
                .Where(pc => pc.CompanyId == request.CompanyId && pc.BranchId == access.Profile.BranchId)
                .OrderByDescending(pc => pc.CreatedAt)
                .ToListAsync(ct);

            if (physicalCounts.Count == 0)
                return [];

            var mapped = mapper.Map<List<PhysicalCountListItemDto>>(physicalCounts);

            var userIds = physicalCounts
                .Select(pc => pc.CreatedBy)
                .Distinct()
                .ToList();

            var users = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.Fullname ?? u.UserName ?? u.Email })
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

            for (var i = 0; i < physicalCounts.Count; i++)
            {
                if (users.TryGetValue(physicalCounts[i].CreatedBy, out var name))
                    mapped[i].CreatedByName = name;
            }

            return mapped;
        }
    }
}
