using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Validators
{
    public class CreateReceptionEntranceValidator : AbstractValidator<CreateReceptionEntranceCommand>
    {
        public CreateReceptionEntranceValidator()
        {
            RuleFor(x => x.GeneralInformation)
                .NotNull();
            
            RuleFor(x => x.TransportInformation)
                .NotNull();

            // Tipo de documento: DUCA
            When(x => x.GeneralInformation.DocumentType == DocumentType.DUCA, () =>
            {
                RuleFor(x => x.GeneralInformation.DucatNumbers)
                    .NotEmpty()
                    .WithMessage("Debe indicar al menos un número de DUCA cuando el tipo de documento es DUCA.");

                RuleForEach(x => x.GeneralInformation.DucatNumbers)
                    .NotEmpty()
                    .WithMessage("Los números de DUCA no pueden estar vacíos.");

                RuleFor(x => x.CustomsDeclarationInformation)
                    .Null()
                    .WithMessage("No debe enviar información de declaración aduanera cuando el tipo de documento es DUCA.");
            });

            // Tipo de documento: Declaración aduanera
            When(x => x.GeneralInformation.DocumentType == DocumentType.CustomsDeclaration, () =>
            {
                RuleFor(x => x.GeneralInformation.DucatNumbers)
                    .Empty()
                    .WithMessage("La lista de DUCA debe estar vacía cuando el tipo de documento es Declaración Aduanera.");

                RuleFor(x => x.CustomsDeclarationInformation)
                    .NotNull()
                    .WithMessage("Debe enviar la información de declaración aduanera.");

                RuleFor(x => x.GeneralInformation.CustomsDeclarationNumber)
                    .NotEmpty()
                    .WithMessage("Debe indicar el número de declaración aduanera.");

                When(x => x.CustomsDeclarationInformation is not null, () =>
                {
                    RuleFor(x => x.CustomsDeclarationInformation!.TotalWeight)
                        .GreaterThan(0)
                        .WithMessage("El peso total debe ser mayor que cero.");

                    RuleFor(x => x.CustomsDeclarationInformation!.PackageNumber)
                        .GreaterThan(0)
                        .WithMessage("El número de bultos debe ser mayor que cero.");
                });
            });
        }
    }
}