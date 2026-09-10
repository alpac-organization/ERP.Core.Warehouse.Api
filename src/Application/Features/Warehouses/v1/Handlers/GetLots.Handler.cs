using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class GetLotsBySectionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<GetLotsBySectionQuery, PagedResponse<LotListItemDto>>(unitOfWork, errorManager)
{
    private readonly IMapper _mapper = mapper;

    public override async Task<PagedResponse<LotListItemDto>> Handle(
        GetLotsBySectionQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId,
            request.CompanyId,
            request.ModuleCode,
            cancellationToken);
        if (!access.IsSuccess)
            return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == request.SectionId && s.IsActive && s.DeletedAt == null,
                cancellationToken);
        if (section is null)
            return _errorManager.ThrowBadRequest<PagedResponse<LotListItemDto>>(
                "La sección indicada no existe o no está activa.", "ERP:SECTION_NOT_FOUND");

        if (section.WarehouseId != request.WarehouseId)
            return _errorManager.ThrowBadRequest<PagedResponse<LotListItemDto>>(
                "La sección no pertenece al almacén indicado.", "ERP:SECTION_WAREHOUSE_MISMATCH");

        var queryLots = _unitOfWork.Lots.Entities
            .AsNoTracking()
            .Where(lot => lot.SectionId == request.SectionId && lot.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var codeFilter = request.Code.Trim().ToLower();
            queryLots = queryLots.Where(lot => lot.Code.Contains(codeFilter, StringComparison.CurrentCultureIgnoreCase));
        }

        if (request.RackStatus.HasValue)
            queryLots = queryLots.Where(lot => lot.Status == request.RackStatus.Value);

        var totalRecords = await queryLots.CountAsync(cancellationToken);

        var lots = await queryLots
            .OrderBy(lot => lot.Code)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var lotItems = _mapper.Map<List<LotListItemDto>>(lots);

        return new PagedResponse<LotListItemDto>(
            lotItems,
            request.PageNumber,
            request.PageSize,
            totalRecords);
    }
}