using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class CreateReceptionEntranceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager) : BaseValidatorHandler<CreateReceptionEntranceCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(CreateReceptionEntranceCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
        {
            return access.ErrorResponse!;
        }

        var receptionEntranceEntity = ReceptionEntranceMapper.ToReceptionEntranceEntity(request);



        await _unitOfWork.ReceptionEntrance.InsertReceptionEntrance(receptionEntranceEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}