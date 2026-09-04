using FluentValidation;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Validators
{
    public class GetPurchaseRequestDocumentValidator : AbstractValidator<GetPurchaseRequestDocumentQuery>
    {
        public GetPurchaseRequestDocumentValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("El identificador de usuario es obligatorio.")
                .NotEqual(Guid.Empty).WithMessage("El identificador de usuario no es válido.");

            RuleFor(x => x.CompanyId)
                .NotEmpty().WithMessage("El id de la empresa no puede estar vacío.")
                .NotEqual(Guid.Empty).WithMessage("El id de la empresa es requerido.");

            RuleFor(x => x.ModuleCode)
                .NotEmpty().WithMessage("El código del módulo no puede estar vacío.");

            RuleFor(x => x.DocumentType)
                .IsInEnum().WithMessage("El tipo de documento no es válido.");

            RuleFor(x => x)
                .Must(x => x.DocumentType == PurchaseRequestType.Monthly || x.PurchaseRequestId.HasValue)
                .WithMessage("Para este tipo de documento debe indicarse la solicitud de compra.");
        }
    }
}
