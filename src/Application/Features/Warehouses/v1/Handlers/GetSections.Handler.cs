using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetSectionsHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSectionsQuery, List<SectionDto>>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<List<SectionDto>> Handle(GetSectionsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var sections = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .Where(s => s.IsActive && s.WarehouseId == request.WarehouseId)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<SectionDto>>(sections);
    }
}