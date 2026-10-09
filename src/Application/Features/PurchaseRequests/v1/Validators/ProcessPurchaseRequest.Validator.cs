using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class ProcessPurchaseRequestValidator : AbstractValidator<ProcessPurchaseRequestCommand>
    {
        public ProcessPurchaseRequestValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty)
                .WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.PurchaseRequestId)
                .NotEqual(Guid.Empty)
                .WithMessage("El identificador de la solicitud de compra no es válido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty()
                .WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.NewStatus)
                .IsInEnum()
                .WithMessage("El nuevo estado de la solicitud no es válido.");

            RuleFor(x => x.ReasonRejectionId)
                .NotNull()
                .WithMessage("El motivo de rechazo del catálogo es obligatorio cuando la solicitud es rechazada.")
                .GreaterThan(0)
                .WithMessage("El identificador del motivo de rechazo no es válido.")
                .When(x => x.NewStatus == PurchaseRequestStatus.Rejected);

            RuleFor(x => x.RejectionComments)
                .MaximumLength(1000)
                .WithMessage("Los comentarios de rechazo no pueden exceder los 1000 caracteres.")
                .When(x => !string.IsNullOrWhiteSpace(x.RejectionComments));
        }
    }
}
