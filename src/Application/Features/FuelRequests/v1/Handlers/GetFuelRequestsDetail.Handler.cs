using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.FuelRequests.v1.Handlers;

public class GetFuelRequestsDetailHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetFuelRequestsDetailQuery, GetFuelRequestDetailDto>(unitOfWork, errorManager)
{
    public override async Task<GetFuelRequestDetailDto> Handle(GetFuelRequestsDetailQuery request, CancellationToken ct)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var fuelRequestDetail = await _unitOfWork.FuelRequests.Entities
            .Include(fr => fr.User)
            .Include(fr => fr.WorkArea)
            .Include(fr => fr.CostCenter)
            .Include(fr => fr.Branch)
            .Include(fr => fr.Machinery)
            .Include(fr => fr.ProductStock)
                .ThenInclude(ps => ps.Product)
            .Include(fr => fr.UnitMeasure)
            .Where(fr => fr.Id == request.FuelRequestId && fr.DeletedAt == null)
            .FirstOrDefaultAsync(ct);

        if (fuelRequestDetail is null) return _errorManager.ThrowBadRequest<GetFuelRequestDetailDto>(
            "La solicitud de combustible no existe",
            "ERP:FUEL_REQUEST_NOT_FOUND");

        var approvedByUserChief = fuelRequestDetail.ApprovedByUserChiefId.HasValue
            ? await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == fuelRequestDetail.ApprovedByUserChiefId.Value, ct)
            : null;

        var annulledByUserChief = fuelRequestDetail.AnnulledByUserChiefId.HasValue
            ? await _unitOfWork.Users.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == fuelRequestDetail.AnnulledByUserChiefId.Value, ct)
            : null;

        var fuelRequestMapped = mapper.Map<GetFuelRequestDetailDto>(fuelRequestDetail);

        fuelRequestMapped.ApprovedByUserChiefName = approvedByUserChief?.Fullname;
        fuelRequestMapped.AnnulledByUserChiefName = annulledByUserChief?.Fullname;

        return fuelRequestMapped;
    }
}