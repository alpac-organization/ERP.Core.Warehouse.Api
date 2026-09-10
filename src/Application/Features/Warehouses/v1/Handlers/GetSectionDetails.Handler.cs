using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

/**
    Obtener detalles de secciones, información de coordenadas, información de capacidad y espacio disponible
*/
public class GetSectionDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetSectionDetailsQuery, SectionDetailsDto>(_unitOfWork, _errorManager)
{
    public override async Task<SectionDetailsDto> Handle(GetSectionDetailsQuery request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess) return access.ErrorResponse!;

        var section = await _unitOfWork.Sections.Entities
            .Include(sec => sec.SectionCapacity)
            .FirstOrDefaultAsync(sec => sec.Id == request.SectionId, cancellationToken);

        if (section is null)
        {
            return _errorManager.ThrowBadRequest<SectionDetailsDto>("No se encontro el detalle de esta solicitud", "ERP:NOT_FOUND");
        }

        return _mapper.Map<SectionDetailsDto>(section);
    }
}
