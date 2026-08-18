using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Queries;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.ShippingCompanies.v1.Handlers;

public class GetShippingCompaniesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetShippingCompaniesQuery, List<ShippingCompanyDto>>(unitOfWork, errorManager)
{
    public override async Task<List<ShippingCompanyDto>> Handle(GetShippingCompaniesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var shippingCompanies = await _unitOfWork.ShippingComapanies.Entities
            .AsNoTracking()
            .Where(sc => sc.DeletedAt == null)
            .OrderBy(sc => sc.Name)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<ShippingCompanyDto>>(shippingCompanies);
    }
}