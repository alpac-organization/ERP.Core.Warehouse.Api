using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers
{
    public class CreatePhysicalCountHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
        : BaseValidatorHandler<CreatePhysicalCountCommand, CreatePhysicalCountResponse>(unitOfWork, errorManager)
    {
        public override async Task<CreatePhysicalCountResponse> Handle(CreatePhysicalCountCommand request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
            if (!access.IsSuccess)
                return access.ErrorResponse!;

            var branchId = access.Profile.BranchId;
            request.BranchId = branchId;

            var productStocks = await _unitOfWork.ProductStocks.Entities
                .AsNoTracking()
                .Where(ps => ps.Product.BranchId == branchId)
                .ToListAsync(ct);

            request.TotalLines = productStocks.Count;

            var physicalCount = mapper.Map<PhysicalCount>(request);

            var created = await _unitOfWork.PhysicalCounts.RegisterPhysicalCount(physicalCount);

            foreach (var productStock in productStocks)
            {
                var line = mapper.Map<PhysicalCountLine>(productStock);
                line.PhysicalCountId = created.Id;

                await _unitOfWork.PhysicalCountLines.RegisterPhysicalCountLine(line);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return new CreatePhysicalCountResponse { PhysicalCountId = created.Id };
        }
    }
}
