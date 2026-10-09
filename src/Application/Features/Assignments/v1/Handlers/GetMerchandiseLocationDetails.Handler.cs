using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class GetMerchandiseLocationDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetMerchandiseLocationDetailsQuery, MerchandiseLocationDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<MerchandiseLocationDetailsDto> Handle(GetMerchandiseLocationDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Include(a => a.Codes)
                .Include(a => a.Warehouse)
                .Include(a => a.OperationalOrder)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(a => a.Section)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(a => a.LotPosition)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(a => a.RackPosition)
                .Where(a => a.Codes
                    .Any(c => 
                        c.CodeGenerated == request.AssignmentCode
                    )
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (assignment is null)
            {
                return _errorManager.ThrowBadRequest<MerchandiseLocationDetailsDto>("El codigo de asignamiento es invalido","ERP:01");
            }

            var assignmentMappedInformation = _mapper.Map<MerchandiseLocationDetailsDto>(assignment);

            return assignmentMappedInformation;
        }
    }
}