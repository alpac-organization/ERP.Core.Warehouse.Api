using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class AssignMerchandiseDesignatedLocationValidator : BaseRequestValidator<AssignMerchandiseDesignatedLocationCommand>
    {
        public AssignMerchandiseDesignatedLocationValidator()
        {
            RuleFor(x => x)
                .Must(x => x.Sections.Count > 0 || x.MerchandiseType.HasValue || x.Pallets is { Count: > 0 })
                .WithMessage("Debe indicar posiciones a asignar, el tipo de mercadería o al menos un registro de polines.");

            RuleFor(x => x.MerchandiseType)
                .IsInEnum().WithMessage("El tipo de mercadería no es válido.")
                .When(x => x.MerchandiseType.HasValue);

            RuleFor(x => x.Pallets)
                .NotEmpty().WithMessage("Debe indicar al menos un registro de polines.")
                .When(x => x.Pallets is not null);

            RuleForEach(x => x.Pallets)
                .ChildRules(p =>
                {
                    p.RuleFor(p => p.Type)
                        .IsInEnum().WithMessage("El tipo de polín no es válido.");

                    p.RuleFor(p => p.CountPallets)
                        .GreaterThan(0).WithMessage("La cantidad de polines debe ser mayor que cero.");

                    p.RuleFor(p => p.Width)
                        .GreaterThan(0).WithMessage("El largo del polín sobredimensionado debe ser mayor que cero.")
                        .When(p => p.Type == PalletType.Oversized && p.Width.HasValue);

                    p.RuleFor(p => p.Length)
                        .GreaterThan(0).WithMessage("El ancho del polín sobredimensionado debe ser mayor que cero.")
                        .When(p => p.Type == PalletType.Oversized && p.Length.HasValue);

                    p.RuleFor(p => p.BulksPerPallet)
                        .GreaterThan(0).WithMessage("La cantidad de bultos por polín debe ser mayor que cero.")
                        .When(p => p.BulksPerPallet.HasValue);
                });

            When(x => x.Sections.Count > 0, () =>
            {
                RuleFor(x => x.Sections)
                    .NotEmpty()
                    .WithMessage("Debe indicar al menos una sección con posiciones a asignar.");

                RuleFor(x => x)
                    .Must(x => !HasDuplicatedPositions(x))
                    .WithMessage("No puede asignar la misma posición más de una vez.");

                RuleForEach(x => x.Sections)
                    .SetValidator(new AssignMerchandiseSectionValidator());
            });
        }
        private static bool HasDuplicatedPositions(AssignMerchandiseDesignatedLocationCommand request)
        {
            var ids = request.Sections
                .SelectMany(s => s.Tramos.Concat(s.Racks))
                .SelectMany(b => b.PositionIds)
                .ToList();

            return ids.Distinct().Count() != ids.Count;
        }
    }

    public class AssignMerchandiseSectionValidator : AbstractValidator<AssignMerchandiseSectionDto>
    {
        public AssignMerchandiseSectionValidator()
        {
            RuleFor(x => x.SectionId)
                .NotEmpty().WithMessage("El id de la sección es requerido.")
                .NotEqual(Guid.Empty).WithMessage("El id de la sección no es válido.");

            When(x => x.Tramos.Count > 0, () =>
            {
                RuleForEach(x => x.Tramos)
                    .ChildRules(b =>
                    {
                        b.RuleFor(b => b.BlockId)
                            .NotEmpty().WithMessage("El id del tramo es requerido.");

                        b.RuleFor(b => b.PositionIds)
                            .NotEmpty().WithMessage("El tramo debe indicar al menos una posición.")
                            .Must(ids => ids.All(id => id != Guid.Empty))
                            .WithMessage("Un tramo contiene ids de posición no válidos.");
                    });
            });

            When(x => x.Racks.Count > 0, () =>
            {
                RuleForEach(x => x.Racks)
                    .ChildRules(b =>
                    {
                        b.RuleFor(b => b.BlockId)
                            .NotEmpty().WithMessage("El id del rack es requerido.");

                        b.RuleFor(b => b.PositionIds)
                            .NotEmpty().WithMessage("El rack debe indicar al menos una posición.")
                            .Must(ids => ids.All(id => id != Guid.Empty))
                            .WithMessage("Un rack contiene ids de posición no válidos.");
                    });
            });
        }
    }
}