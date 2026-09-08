

using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using Microsoft.EntityFrameworkCore;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers
{
   public class GetSectionCapacitiesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetSectionCapacitiesQuery, SectionCapacitiesDto>(_unitOfWork, _errorManager)
   {
      public override async Task<SectionCapacitiesDto> Handle(GetSectionCapacitiesQuery request, CancellationToken cancellationToken)
      {
         var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

         if (!access.IsSuccess)
         {
            return access.ErrorResponse!;
         }

         var sectionCapacities = await _unitOfWork.SectionCapacity.Entities
            .Where(cap => cap.SectionId == request.SectionId)
            .FirstOrDefaultAsync(cancellationToken);

         if (sectionCapacities is null)
         {
            return _errorManager.ThrowBadRequest<SectionCapacitiesDto>("No se encontró el detalle de esta sección", "ERP:NOT_FOUND");
         }

         return _mapper.Map<SectionCapacitiesDto>(sectionCapacities);
      }
   }
}