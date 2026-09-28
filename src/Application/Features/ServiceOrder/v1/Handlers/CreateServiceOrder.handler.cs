using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Commands;
namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Handlers
{
    public class CreateServiceOrderHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<CreateServiceOrderCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(CreateServiceOrderCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }


            return Unit.Value;
        }
    }
}