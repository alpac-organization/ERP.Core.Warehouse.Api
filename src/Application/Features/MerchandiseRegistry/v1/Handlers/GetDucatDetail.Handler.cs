using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Handlers;

public class GetDucatDetailHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper)
    : BaseValidatorHandler<GetDucatDetailQuery, GetDucatDetailDto>(unitOfWork, errorManager)
{
    public override async Task<GetDucatDetailDto> Handle(
        GetDucatDetailQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            cancellationToken);

        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var ducat = await _unitOfWork.EntranceDucats.Entities
            .AsNoTracking()
            .Include(d => d.RegistryDetail!)
                .ThenInclude(rd => rd.Merchandise)
            .FirstOrDefaultAsync(
                d => d.Id == request.DucatId && d.DeletedAt == null,
                cancellationToken);

        if (ducat is null)
            return _errorManager.ThrowNotFound<GetDucatDetailDto>(
                "El DUCA no fue encontrado o ya ha sido eliminado.",
                "ERP:DUCAT_NOT_FOUND");

        return mapper.Map<GetDucatDetailDto>(ducat);
    }
}
