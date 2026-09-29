using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetWarehouseByIdHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper)
    : BaseValidatorHandler<GetWarehouseByIdQuery, WarehouseDetailDto>(unitOfWork, errorManager)
{
    public override async Task<WarehouseDetailDto> Handle(
        GetWarehouseByIdQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var warehouse = await _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Include(w => w.WarehouseCapacity)
            .Include(w => w.WarehouseLocation)
            .Include(w => w.Sections.Where(s => s.DeletedAt == null))
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.DeletedAt == null,cancellationToken);

        if (warehouse is null)
        {
            return _errorManager.ThrowBadRequest<WarehouseDetailDto>(
                "El almacén indicado no existe.",
                "ERP:WAREHOUSE_NOT_FOUND");
        }

        return mapper.Map<WarehouseDetailDto>(warehouse);
    }
}
