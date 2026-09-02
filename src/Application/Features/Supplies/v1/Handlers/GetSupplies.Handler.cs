using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Supplies.v1.Handlers;

public class GetSuppliesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSuppliesQuery, List<SupplyDto>>(unitOfWork, errorManager)
{
    public override async Task<List<SupplyDto>> Handle(GetSuppliesQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var supplies = await _unitOfWork.Supplies.Entities
            .AsNoTracking()
            .Where(s => s.DeletedAt == null && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<SupplyDto>>(supplies);
    }
}
