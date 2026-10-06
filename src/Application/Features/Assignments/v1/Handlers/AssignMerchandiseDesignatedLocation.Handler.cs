using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class AssignMerchandiseDesignatedLocationHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<AssignMerchandiseDesignatedLocationCommand, AssignMerchandiseDesignatedLocationDto>(_unitOfWork, _errorManager)
    {
        public override async Task<AssignMerchandiseDesignatedLocationDto> Handle(AssignMerchandiseDesignatedLocationCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }








            return new();
        }
    }
}