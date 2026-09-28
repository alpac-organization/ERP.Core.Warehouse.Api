using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Handlers;

public class GetOperationalServiceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager,
    IMapper _mapper) : BaseValidatorHandler<GetOperationalServicesQuery, PagedResponse<GetOperationalServiceDto>>(_unitOfWork, _errorManager)
{
    public override async Task<PagedResponse<GetOperationalServiceDto>> Handle(GetOperationalServicesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var servicesQuery = _unitOfWork.OperationalServices.Entities
            .AsNoTracking()
            .Where(s => s.DeletedAt == null && s.IsActive == true);

        if (!string.IsNullOrEmpty(request.ServiceCode))
        {
            var pattern = $"%{request.ServiceCode.Trim()}%";
            servicesQuery = servicesQuery.Where(s => EF.Functions.ILike(s.ServiceCode!, pattern));
        }

        if (!string.IsNullOrEmpty(request.ServiceName))
        {
            var pattern = $"%{request.ServiceName.Trim()}%";
            servicesQuery = servicesQuery.Where(s => EF.Functions.ILike(s.ServiceName!, pattern));
        }

        var totalRecords = await servicesQuery.CountAsync(cancellationToken);

        var servicesRequest = await servicesQuery
            .OrderBy(s => s.ServiceCode)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var serviceRequestMapped = _mapper.Map<List<GetOperationalServiceDto>>(servicesRequest);

        return new PagedResponse<GetOperationalServiceDto>(
            serviceRequestMapped,
            request.PageNumber,
            request.PageSize,
            totalRecords
        );
    }
}