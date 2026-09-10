using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class DeleteLotCommandValidator : BaseRequestValidator<DeleteLotCommand>
{
    public DeleteLotCommandValidator()
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la sección no es válido.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id del almacén no es válido.");

        RuleFor(x => x.LotId)
            .NotEmpty()
            .WithMessage("El id del tramo es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id del tramo no es válido.");
    }
}