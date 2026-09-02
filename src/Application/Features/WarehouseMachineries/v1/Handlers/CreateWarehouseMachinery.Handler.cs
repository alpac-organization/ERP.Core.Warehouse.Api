using System;
using System.Threading;
using System.Threading.Tasks;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Handlers
{
    public class CreateWarehouseMachineryHandler : BaseValidatorHandler<CreateWarehouseMachineryCommand, bool>
    {
        public CreateWarehouseMachineryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
            : base(unitOfWork, errorManager)
        {
        }

        public override async Task<bool> Handle(CreateWarehouseMachineryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var machinery = new WarehouseMachinery
            {
                Id = Guid.NewGuid(),
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                WarehouseId = request.WarehouseId,
                AssignedOperatorId = request.AssignedOperatorId,
                
                Code = request.Code,
                SerialNumber = request.SerialNumber,
                LicensePlate = request.LicensePlate,
                Name = request.Name,
                Brand = request.Brand,
                Model = request.Model,
                ManufactureYear = request.ManufactureYear,
                
                MachineryType = request.MachineryType,
                FuelType = request.FuelType,
                LoadCapacityKg = request.LoadCapacityKg,
                MaxReachHeightMeters = request.MaxReachHeightMeters,
                HourMeter = request.HourMeter,
                
                Status = request.Status,
                Notes = request.Notes,
                PurchaseDate = request.PurchaseDate,
                
                IsActive = true
            };

            await _unitOfWork.WarehouseMachineries.RegisterMachinery(machinery);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
