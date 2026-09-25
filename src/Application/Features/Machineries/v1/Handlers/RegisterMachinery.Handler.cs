using MediatR;
using Microsoft.Extensions.Logging;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Handlers;

public class RegisterMachineryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, ILogger<RegisterMachineryHandler> logger) : BaseValidatorHandler<MachineryCommand, Unit>(unitOfWork, errorManager)
{
    public override async Task<Unit> Handle(MachineryCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚀Iniciando registro de maquinaria.");

        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        if (access.Role?.RoleType == RoleType.Administrator)
         return _errorManager.ThrowBadRequest<Unit>("No tienes acceso para realizar esta acción","ERP:INVALID_ACCESS");

        var branch = access.Profile.BranchId;

        var machinery = MachineryProfile.ToMachineryEntity(request, branch);

        await _unitOfWork.Machineries.RegisterMachinery(machinery);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Maquinaria registrada con éxito✅");
        return Unit.Value;
    }
}

