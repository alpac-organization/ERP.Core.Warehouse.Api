using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class UpdateAssignmentPositionsValidator : BaseRequestValidator<UpdateAssignmentPositionsCommand>
    {
        public UpdateAssignmentPositionsValidator()
        {
            RuleFor(x => x.MerchandiseType)
                .IsInEnum().WithMessage("El tipo de mercadería no es válido.");

            RuleFor(x => x.Pallets)
                .NotEmpty().WithMessage("Debe indicar al menos un registro de polines.");

            RuleForEach(x => x.Pallets)
                .ChildRules(p =>
                {
                    p.RuleFor(p => p.Type)
                        .IsInEnum().WithMessage("El tipo de polín no es válido.");

                    p.RuleFor(p => p.CountPallets)
                        .GreaterThan(0).WithMessage("La cantidad de polines debe ser mayor que cero.");

                    p.RuleFor(p => p.Width)
                        .GreaterThan(0).WithMessage("El largo del polín sobredimensionado debe ser mayor que cero.")
                        .When(p => p.Type == PalletType.Oversized);

                    p.RuleFor(p => p.Length)
                        .GreaterThan(0).WithMessage("El ancho del polín sobredimensionado debe ser mayor que cero.")
                        .When(p => p.Type == PalletType.Oversized);
                });

            RuleForEach(x => x.Pallets)
                .Must((command, pallet) => pallet.BulksPerPallet is > 0)
                .WithMessage("La cantidad de bultos por polín debe ser mayor que cero.")
                .When(x => x.MerchandiseType == UnloadingMerchandiseType.Bulk);

            RuleForEach(x => x.Pallets)
                .Must((command, pallet) => pallet.BulksPerPallet is null)
                .WithMessage("La cantidad de bultos por polín no aplica para mercadería armada.")
                .When(x => x.MerchandiseType == UnloadingMerchandiseType.Armed);
        }
    }
}