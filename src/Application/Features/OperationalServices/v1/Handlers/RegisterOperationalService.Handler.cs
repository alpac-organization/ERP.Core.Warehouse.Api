using MediatR;
using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using System.Text.RegularExpressions;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Handlers;

public class RegisterOperationalServiceHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager,
    ILogger<RegisterOperationalServiceHandler> logger) : BaseValidatorHandler<RegisterOperationalServicesCommand, Unit>(_unitOfWork, _errorManager)
{
    public override async Task<Unit> Handle(RegisterOperationalServicesCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("🚀Iniciando registro de servicios.");

        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse;

        var services = OperationalServiceProfile.ToOperationalServiceEntity(request);

        await _unitOfWork.OperationalServices.RegisterOperationalService(services);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Servicio registrado con éxito✅.");

        return Unit.Value;
    }
}