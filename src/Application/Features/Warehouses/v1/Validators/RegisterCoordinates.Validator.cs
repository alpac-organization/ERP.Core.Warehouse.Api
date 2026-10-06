using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Validators;

public class RegisterCoordinatesValidator : AbstractValidator<RegisterCoordinatesCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCoordinatesValidator(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;

        RuleFor(x => x.WarehouseId)
            .NotEmpty()
            .WithMessage("El id del almacén es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id del almacén no es válido.");

        RuleFor(x => x.SectionId)
            .NotEmpty()
            .WithMessage("El id de la sección es requerido.")
            .NotEqual(Guid.Empty)
            .WithMessage("El id de la sección no es válido.");


       RuleFor(x => x.RackId)
            .NotEmpty()
            .WithMessage("El id del rack es requerido.")
            .When(x => x.TargetType == CoordinateTargetType.RackPositions);

        RuleFor(x => x.LotId)
            .NotEmpty()
            .WithMessage("El id del tramo es requerido.")
            .When(x => x.TargetType == CoordinateTargetType.LotsPositions);


        RuleFor(x => x.LotsPositionsInformation)
            .NotEmpty()
            .When(x => x.TargetType == CoordinateTargetType.LotsPositions)
            .WithMessage("Debe proporcionar al menos una posición de tramo para registrar coordenadas.");

        RuleFor(x => x.RackPositionsInformation)
            .NotEmpty()
            .When(x => x.TargetType == CoordinateTargetType.RackPositions)
            .WithMessage("Debe proporcionar al menos una posición de rack para registrar coordenadas.");

        RuleForEach(x => x.LotsPositionsInformation)
            .ChildRules(position =>
            {
                position.RuleFor(p => p.LotPositionId)
                    .NotEmpty()
                    .WithMessage("El id de la posición del tramo es requerido.")
                    .NotEqual(Guid.Empty)
                    .WithMessage("El id de la posición del tramo no es válidp.")
                    .MustAsync(BeValidLotPositionAsync)
                    .WithMessage("La posición de tramo no existe o no pertenece a la sección indicada.");

                position.RuleFor(p => p.PositionX)
                    .NotNull().WithMessage("La coordenada X es requerida.")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 6 decimales");

                position.RuleFor(p => p.PositionY)
                    .NotNull().WithMessage("La coordenada Y es requerida.")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 6 decimales");

                position.RuleFor(p => p.PositionZ)
                    .NotNull().WithMessage("La coordenada Z es requerida.")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Z admite máximo 6 decimales");

                position.RuleFor(p => p.RotationY)
                    .NotNull().WithMessage("La rotación en Y es requerida.")
                    .InclusiveBetween(0, 360).WithMessage("La rotación Y debe estar entre 0 y 360")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La rotación Y admite máximo 6 decimales");
            });

        RuleForEach(x => x.RackPositionsInformation)
            .ChildRules(position =>
            {
                position.RuleFor(p => p.RackPositionId)
                    .NotEmpty()
                    .WithMessage("El id de la posición del rack es requerido.")
                    .NotEqual(Guid.Empty)
                    .WithMessage("El id de la posición del rack no es válido.")
                    .MustAsync(BeValidRackPositionAsync)
                    .WithMessage("La posición de rack no existe o no pertenece a la sección indicada.");

                position.RuleFor(p => p.PositionX)
                    .NotNull().WithMessage("La coordenada X es requerida.")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada X admite máximo 6 decimales");

                position.RuleFor(p => p.PositionY)
                    .NotNull().WithMessage("La coordenada Y es requerida.")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Y admite máximo 6 decimales");

                position.RuleFor(p => p.PositionZ)
                    .NotNull().WithMessage("La coordenada Z es requerida.")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La coordenada Z admite máximo 6 decimales");

                position.RuleFor(p => p.RotationY)
                    .NotNull().WithMessage("La rotación en Y es requerida.")
                    .InclusiveBetween(0, 360).WithMessage("La rotación Y debe estar entre 0 y 360")
                    .PrecisionScale(18, 6, ignoreTrailingZeros: true).WithMessage("La rotación Y admite máximo 6 decimales");
            });
    }

    private async Task<bool> BeValidLotPositionAsync(Guid lotPositionId, CancellationToken cancellationToken)
    {
        var lotPosition = await _unitOfWork.LotsPositions.Entities
            .Include(lp => lp.Lot)
            .FirstOrDefaultAsync(lp => lp.Id == lotPositionId && lp.DeletedAt == null, cancellationToken);

        if (lotPosition is null)
            return false;

        return lotPosition.Lot?.SectionId != null;
    }

    private async Task<bool> BeValidRackPositionAsync(Guid rackPositionId, CancellationToken cancellationToken)
    {
        var rackPosition = await _unitOfWork.RackPositions.Entities
            .Include(rp => rp.Rack)
            .FirstOrDefaultAsync(rp => rp.Id == rackPositionId && rp.DeletedAt == null, cancellationToken);

        if (rackPosition is null)
            return false;

        return rackPosition.Rack?.SectionId != null;
    }
}