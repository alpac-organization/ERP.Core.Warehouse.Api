using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Handlers;

public class GetSupplyRequestsDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetSupplyRequestsDetailQuery, GetSupplyRequestDetailDto>(unitOfWork, errorManager)
{
    public override async Task<GetSupplyRequestDetailDto> Handle(GetSupplyRequestsDetailQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var supplyRequestsDetail = await _unitOfWork.SupplyRequests.Entities
            .Include(sr => sr.User)
            .Include(sr => sr.WorkArea)
            .Include(sr => sr.CostCenter)
            .Include(sr => sr.Branch)
            .Include(sr => sr.Warehouse)
            .Include(sr => sr.OperationalOrder)
            .Include(sr => sr.SupplyRequestItems)
                .ThenInclude(sri => sri.ProductStock)
                    .ThenInclude(ps => ps.Product)
            .Where(sr => sr.Id == request.SupplyRequestId && sr.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (supplyRequestsDetail is null) return _errorManager.ThrowBadRequest<GetSupplyRequestDetailDto>(
            "La solicitud de suministro no existe",
            "ERP:SUPPLY_REQUEST_NOT_FOUND");

        var supplyRequestsMapped = mapper.Map<GetSupplyRequestDetailDto>(supplyRequestsDetail);

        return supplyRequestsMapped;

    }
}