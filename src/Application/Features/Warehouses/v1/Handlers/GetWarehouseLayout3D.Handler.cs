using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetWarehouseLayout3DHandler(IUnitOfWork unitOfWork, IErrorManager errorManager)
    : BaseValidatorHandler<GetWarehouseLayout3dQuery, WarehouseLayout3dDto>(unitOfWork, errorManager)
{
    public override async Task<WarehouseLayout3dDto> Handle(GetWarehouseLayout3dQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var layout = await _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .Where(w => w.Id == request.WarehouseId)
            .Select(w => new WarehouseLayout3dDto
            {
                WarehouseId = w.Id,
                Code = w.Code,
                WidthMetres = w.Details.WitdhMetres,
                LengthMetres = w.Details.LengthMetres,
                Sections = w.Sections.Where(s => s.IsActive).Select(s => new SectionLayout3dDto
                {
                    SectionId = s.Id,
                    Code = s.Code,
                    SectionType = s.SectionType,
                    StorageType = s.StorageType,
                    WidthMetres = s.WidthMetres,
                    LengthMetres = s.LengthMetres,
                    Transform = new LayoutTransform3DDto
                    {
                        PositionX = s.TransformWarehouse3D.PositionX,
                        PositionY = s.TransformWarehouse3D.PositionY,
                        PositionZ = s.TransformWarehouse3D.PositionZ,
                        RotationY = s.TransformWarehouse3D.RotationY
                    },
                    Lots = (s.StorageType == SectionStorageType.Lots)
                    ? s.Lots.Select(lot => new LotLayout3DDto
                    {
                        LotId = lot.Id,
                        Code = lot.Code,
                        WidthMetres = lot.WidthMetres,
                        LengthMetres = lot.LengthMetres,
                        Transform = new LayoutTransform3DDto
                        {
                            PositionX = lot.TransformWarehouse3D.PositionX,
                            PositionY = lot.TransformWarehouse3D.PositionY,
                            PositionZ = lot.TransformWarehouse3D.PositionZ,
                            RotationY = lot.TransformWarehouse3D.RotationY
                        }
                    }).ToList()
                    : new List<LotLayout3DDto>(),

                    Racks = (s.StorageType == SectionStorageType.Racks)
                    ? s.Racks.Select(r => new RackLayout3dDto
                    {
                        RackId = r.Id,
                        Code = r.Code,
                        WidthMetres = r.WidthMetres,
                        LengthMetres = r.LengthMetres,
                        HeightMetres = r.HeightMetres,
                        Transform = new LayoutTransform3DDto
                        {
                            PositionX = r.TransformWarehouse3D.PositionX,
                            PositionY = r.TransformWarehouse3D.PositionY,
                            PositionZ = r.TransformWarehouse3D.PositionZ,
                            RotationY = r.TransformWarehouse3D.RotationY
                        }
                    }).ToList()
                    : new List<RackLayout3dDto>()
                }).ToList()
            }).FirstOrDefaultAsync(cancellationToken);

        if (layout is null)
            return _errorManager.ThrowNotFound<WarehouseLayout3dDto>("La bodega no existe.", "ERP:WAREHOUSE_NOT_FOUND");

        return layout;
    }
}