using AutoMapper;
using AutoMapper.QueryableExtensions;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Handlers;

public class GetMerchandisesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager,
    IMapper mapper)
    :   BaseValidatorHandler<GetMerchandisesQuery, List<MerchandiseDto>>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<List<MerchandiseDto>> Handle(GetMerchandisesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var query = _unitOfWork.Merchandises.Entities
            .AsNoTracking()
            .Where(c => !c.DeletedAt.HasValue);
        
        if(request.CategoryProductId.HasValue)
            query  = query.Where(c => c.CategoryId == request.CategoryProductId.Value);

        var merchandises = await query
            .OrderBy(p => p.MerchandiseName)
            .ProjectTo<MerchandiseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return merchandises;
    }
}