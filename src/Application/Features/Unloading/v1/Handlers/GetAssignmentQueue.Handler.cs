using AutoMapper;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Handlers;

public class GetAssignmentQueueHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetAssignmentQueueQuery, GetAssignmentQueueDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<GetAssignmentQueueDto> Handle(
        GetAssignmentQueueQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var query = _unitOfWork.WarehouseAssignments.Entities
            .AsNoTracking()
            .Where(a => a.DeletedAt == null && a.UnloadingStatus == (request.UnloadingStatus ?? UnloadingStatus.Pending));

        if (!string.IsNullOrWhiteSpace(request.ServiceOrderCode))
        {
            var filter = request.ServiceOrderCode.Trim().ToLower().Replace(" ", "");
            query = query.Where(a => a.EntranceDucat!.ServiceOrderCode!.ToLower().Replace(" ", "").Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(request.DucatNumber))
        {
            var filter = request.DucatNumber.Trim().ToLower().Replace(" ", "");
            query = query.Where(a => a.EntranceDucat!.DucatNumber.ToLower().Replace(" ", "").Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(request.WarehouseName))
        {
            var filter = request.WarehouseName.Trim().ToLower().Replace(" ", "");
            query = query.Where(a => a.Warehouse!.WarehouseName.ToLower().Replace(" ", "").Contains(filter));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var data = await query
            .OrderByDescending(a => a.AssignedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<AssignmentQueueItemDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new GetAssignmentQueueDto
        {
            Data = data,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}