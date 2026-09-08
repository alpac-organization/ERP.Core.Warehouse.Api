using FluentValidation;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

/// <summary>
/// Change Name => RegisterLocValidator
/// </summary>
public class RegisterLotCommandValidator : AbstractValidator<RegisterLotCommand>
{
    public RegisterLotCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("La sección es obligatoria.");
        // valuidator

    }
}
