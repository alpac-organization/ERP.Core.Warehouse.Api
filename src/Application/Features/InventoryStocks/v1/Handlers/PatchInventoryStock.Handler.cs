using MediatR;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryStocks.v1.Handlers
{
    public class PatchInventoryStockHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<PatchInventoryStockCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(PatchInventoryStockCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            //Your logic to patch here...
            

            
            return Unit.Value;
        }
    }
}