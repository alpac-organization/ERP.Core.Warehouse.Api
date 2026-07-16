using System.Data;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using FluentValidation;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

public class CreateReceptionEntrancecommand : BaseRequest, IRequest<bool>
{
    public Guid WarehouseId {get;set;}
    public Guid? ServiceOrderId { get; set; }
    public int WorkflowStepDefinitionId { get; set; }

    public List<string> DucatNumbers { get; set; } = [];

    public string CountryOfOrigin { get; set; } = string.Empty;
    public string Aduana { get; set; } = string.Empty;
    public DateTime GateEntranceTime { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string TrailerChassis { get; set; } = string.Empty;
    public string DriverLicense { get; set; } = string.Empty;
    public string Transportista { get; set; } = string.Empty;
    public string Medio { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string Consignee { get; set; } = string.Empty;
    public string SealNumber { get; set; } = string.Empty;

    public DateTime StarTime { get; set; }
}

public class CreateREceptionentranceValidator : AbstractValidator<CreateReceptionEntrancecommand>
{
    public CreateREceptionentranceValidator()
    {
        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("El identificador de la bodega es obligatorio.");
        
        RuleFor(x => x.WorkflowStepDefinitionId)
            .GreaterThan(0).WithMessage("El identificador del paso del flujo debe ser mayor a 0.");
        
        RuleFor(x => x.DucatNumbers)
            .NotEmpty().WithMessage("El número de Duca es un dato obligatorio.");

        RuleFor(x => x.StarTime)
            .NotEmpty().WithMessage("La hora de inicio es obligatoria.");
        
        RuleFor(x => x.CountryOfOrigin)
            .NotEmpty().WithMessage("El país de procedencia es obligatorio.");
        
        RuleFor(x => x.Aduana)
            .NotEmpty().WithMessage("La Aduana de ingreso es obligatoria.");

        RuleFor(x => x.PlateNumber)
            .NotEmpty().WithMessage("El número de placa es obligatorio.");

        RuleFor(x => x.TrailerChassis)
            .NotEmpty().WithMessage("El número de chasis/remolque es obligatorio.");

        RuleFor(x => x.DriverLicense)
            .NotEmpty().WithMessage("La licencia del conductor es obligatoria.");

        RuleFor(x => x.Transportista)
            .NotEmpty().WithMessage("La empresa transportista es requerida.");
        
        RuleFor(x => x.Medio)
            .NotEmpty().WithMessage("El medio de transporte es obligatorio.");
        
        RuleFor(x => x.DriverName)
            .NotEmpty().WithMessage("El nombre del conductor es obligatorio.");

        RuleFor(x => x.Consignee)
            .NotEmpty().WithMessage("El consignatario es obligatorio.");

        RuleFor(x => x.SealNumber)
            .NotEmpty().WithMessage("El número de marchamo es obligatorio.");
    }
}