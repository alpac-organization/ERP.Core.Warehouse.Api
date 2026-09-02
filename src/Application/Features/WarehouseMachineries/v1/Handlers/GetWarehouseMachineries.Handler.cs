using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Handlers
{
    public class GetWarehouseMachineriesHandler : BaseValidatorHandler<GetWarehouseMachineriesQuery, IEnumerable<WarehouseMachineryListDto>>
    {
        public GetWarehouseMachineriesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<IEnumerable<WarehouseMachineryListDto>> Handle(GetWarehouseMachineriesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var machineries = await _unitOfWork.WarehouseMachineries.Entities
                .AsNoTracking()
                .Where(m => m.CompanyId == request.CompanyId && m.DeletedAt == null && m.IsActive)
                .OrderBy(m => m.Code)
                .Select(m => new WarehouseMachineryListDto(
                    m.Id,
                    m.Code,
                    m.SerialNumber,
                    m.LicensePlate,
                    m.Name,
                    m.Brand,
                    m.Model,
                    m.MachineryType.ToString(),
                    m.FuelType.ToString(),
                    m.Status.ToString()
                ))
                .ToListAsync(cancellationToken);

            return machineries;
        }
    }
}
