using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseTasks.v1.Validators;

public class PauseWarehouseTaskValidator : BaseRequestValidator<PauseWarehouseTaskCommand>
{
    public PauseWarehouseTaskValidator()
    {
        RuleFor(x => x.WarehouseTaskId)
            .NotEmpty()
            .WithMessage("La tarea de bodega es requerida.");
    }
}
