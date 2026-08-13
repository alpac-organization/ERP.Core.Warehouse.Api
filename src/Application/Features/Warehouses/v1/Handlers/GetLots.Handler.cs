using Microsoft.EntityFrameworkCore;
using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetLotByIdHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetLotByIdQuery, LotDto>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<LotDto> Handle(GetLotByIdQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var lot = await _mapper.ProjectTo<LotDto>(
                _unitOfWork.Lots.Entities.Where(l =>
                    l.Id == request.LotId && l.SectionId == request.SectionId))
            .FirstOrDefaultAsync(cancellationToken);

        if (lot is null)
            return _errorManager.ThrowBadRequest<LotDto>(
                "El tramo indicado no existe en esta sección.", "ERP:LOT_NOT_FOUND");

        return lot;
    }
}