using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers
{
    public class GetPhysicalCountLinesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
        : BaseValidatorHandler<GetPhysicalCountLinesQuery, PhysicalCountLinesPageDto>(unitOfWork, errorManager)
    {
        public override async Task<PhysicalCountLinesPageDto> Handle(GetPhysicalCountLinesQuery request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
            if (!access.IsSuccess)
                return access.ErrorResponse!;

            var physicalCount = await _unitOfWork.PhysicalCounts.Entities
                .AsNoTracking()
                .Include(pc => pc.Branch)
                .FirstOrDefaultAsync(
                    pc => pc.Id == request.PhysicalCountId && pc.CompanyId == request.CompanyId,
                    ct);

            if (physicalCount == null)
            {
                return _errorManager.ThrowBadRequest<PhysicalCountLinesPageDto>(
                    "Levantamiento fisico no encontrado.", "ERP:007");
            }

            if (physicalCount.BranchId != access.Profile.BranchId)
            {
                return _errorManager.ThrowUnauthorized<PhysicalCountLinesPageDto>(
                    "No tiene acceso a levantamientos fisicos de otra sucursal.",
                    "ERP:INVALID_BRANCH_ACCESS");
            }

            var query = _unitOfWork.PhysicalCountLines.Entities
                .AsNoTracking()
                .Where(l => l.PhysicalCountId == request.PhysicalCountId)
                .Include(l => l.ProductStock)
                    .ThenInclude(ps => ps.Product);

            var totalRecords = await query.CountAsync(ct);

            var lines = await query
                .OrderBy(l => l.PhysicalStock.HasValue ? 1 : 0)
                .ThenBy(l => l.ProductStock.Product.ProductName)
                .ThenBy(l => l.ProductStock.Product.Code)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            var mapped = mapper.Map<List<PhysicalCountLineDto>>(lines);

            var userIds = lines
                .Where(l => l.CountedByUserId.HasValue)
                .Select(l => l.CountedByUserId!.Value)
                .ToList();

            userIds.Add(physicalCount.CreatedBy);
            if (physicalCount.CompletedBy.HasValue)
                userIds.Add(physicalCount.CompletedBy.Value);

            userIds = userIds.Distinct().ToList();

            var users = await _unitOfWork.Users.Entities
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.Fullname ?? u.UserName ?? u.Email })
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

            string? GetUserName(Guid? id) =>
                id.HasValue && users.TryGetValue(id.Value, out var name) ? name : null;

            for (var i = 0; i < lines.Count; i++)
            {
                mapped[i].CountedByName = GetUserName(lines[i].CountedByUserId);
            }

            var summary = mapper.Map<PhysicalCountSummaryDto>(physicalCount);
            summary.CreatedByName = GetUserName(physicalCount.CreatedBy);
            summary.CompletedByName = GetUserName(physicalCount.CompletedBy);

            return new PhysicalCountLinesPageDto
            {
                Data = mapped,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Total = totalRecords,
                PhysicalCount = summary
            };
        }
    }
}
