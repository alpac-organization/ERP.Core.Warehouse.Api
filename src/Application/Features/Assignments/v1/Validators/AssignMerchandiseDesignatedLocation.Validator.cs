using FluentValidation;
using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Validators
{
    public class AssignMerchandiseDesignatedLocationValidator : BaseRequestValidator<AssignMerchandiseDesignatedLocationCommand>
    {
        public AssignMerchandiseDesignatedLocationValidator()
        {
            RuleFor(x => x.Sections)
                .NotEmpty()
                .WithMessage("Debe indicar al menos una sección con posiciones a asignar.");

            RuleFor(x => x)
                .Must(x => !HasDuplicatedPositions(x))
                .WithMessage("No puede asignar la misma posición más de una vez.");

            RuleForEach(x => x.Sections)
                .SetValidator(new AssignMerchandiseSectionValidator());
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