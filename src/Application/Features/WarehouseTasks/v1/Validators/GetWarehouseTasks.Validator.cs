using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Validators;

public class GetWarehouseTasksValidator : AbstractValidator<GetWarehouseTasksQuery>
{
    public GetWarehouseTasksValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la empresa es requerido.");

        RuleFor(x => x.ModuleCode)
            .NotEmpty()
            .WithMessage("El código del módulo es requerido.");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("No se pudo identificar al usuario autenticado.");
    }
}
