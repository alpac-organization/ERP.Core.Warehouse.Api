using MediatR;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryEntries.v1.Handlers
{
    public class RegisterInventoryEntryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<RegisterInventoryEntryCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(RegisterInventoryEntryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowUnauthorized<Unit>("No tiene acceso para realizar esta acción", "ERP:INVALID_ACCESS");
            }

            


            return Unit.Value;
        }
    }
}