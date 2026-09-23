using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class UpdateReceptionEntranceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<UpdateReceptionEntranceCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(UpdateReceptionEntranceCommand request, CancellationToken cancellationToken)
        {

            return true;
        }
    }
}
