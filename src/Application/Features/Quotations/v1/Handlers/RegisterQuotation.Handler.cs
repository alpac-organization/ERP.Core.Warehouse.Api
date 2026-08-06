using Microsoft.Extensions.Logging;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Commands;


namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class RegisterQuotationHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ILogger<RegisterQuotationHandler> _logger) :  BaseValidatorHandler<RegisterQuotationCommand, bool>(_unitOfWork, _errorManager)
    {
        public override async Task<bool> Handle(RegisterQuotationCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<bool>("No tienes permiso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            _logger.LogInformation("🚩Iniciando registro de cotizaciones.");



            _logger.LogInformation("Cotización agregada con exito✅");
            
            return true;
        }
    }
}