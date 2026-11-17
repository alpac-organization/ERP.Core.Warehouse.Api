using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Handlers;

public class GetEquipmentsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<GetEquipmentsQuery, PagedResponse<EquipmentDto>>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<PagedResponse<EquipmentDto>> Handle(GetEquipmentsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var branch = access.Profile.BranchId;

        var equipmentQuery = _unitOfWork.Equipments.Entities
            .Include(e => e.Branch)
            .Where(e => e.DeletedAt == null)
            .AsNoTracking();

        if (access.Role?.RoleType == RoleType.Administrator)
        {
            if (request.BranchId.HasValue)
            {
                equipmentQuery = equipmentQuery.Where(e => e.BranchId == request.BranchId.Value);
            }
        }
        else
        {
            equipmentQuery = equipmentQuery.Where(e => e.BranchId == branch);
        }

        equipmentQuery = ApplyFilters(equipmentQuery, request);

        var totalRecords = await equipmentQuery.CountAsync(cancellationToken);

        var equipment = await equipmentQuery
            .Include(e => e.Branch)
            .Include(e => e.User)
            .OrderBy(e => e.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var equipmentMapped = _mapper.Map<List<EquipmentDto>>(equipment);

        return new PagedResponse<EquipmentDto>(
            equipmentMapped,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );
    }

    #region Filtros
    private static IQueryable<Equipment> ApplyFilters(IQueryable<Equipment> query, GetEquipmentsQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.EquipmentBrand))
        {
            query = query.Where(e => e.Brand.Contains(request.EquipmentBrand));
        }

        if (!string.IsNullOrWhiteSpace(request.EquipmentModel))
        {
            query = query.Where(e => e.Model.Contains(request.EquipmentModel));
        }

        if (!string.IsNullOrWhiteSpace(request.EquipmentCode))
        {
            query = query.Where(e => e.Code.Contains(request.EquipmentCode));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(e => e.Type == request.Type.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == request.IsActive.Value);
        }

        if (request.Gasoline.HasValue)
        {
            query = query.Where(e => e.Gasoline == request.Gasoline.Value);
        }

        if (request.Diesel.HasValue)
        {
            query = query.Where(e => e.Diesel == request.Diesel.Value);
        }

        if (request.Electric.HasValue)
        {
            query = query.Where(e => e.Electric == request.Electric.Value);
        }

        if (request.Lpg.HasValue)
        {
            query = query.Where(e => e.Lpg == request.Lpg.Value);
        }

        return query;
    }
    #endregion
}