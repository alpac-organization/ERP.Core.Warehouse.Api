using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

//Obtener detalles de secciones, CoordenadaInformation, Informacion Capacidade y espacio disponible

/*
    {
        parametros
        parametros
        parametros
        coordenadaInformation: {},
        informationDisponibilidad: {}
    }

    AGREGAR - PATCH:
*/

//GetSectionDetailsHandler

public class GetSectionByIdHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper) : BaseValidatorHandler<GetSectionByIdQuery, SectionDto>(unitOfWork, errorManager)
{
    public override async Task<SectionDto> Handle(
        GetSectionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(
            request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess)
            throw new UnauthorizedAccessException("Acceso denegado.");

        //Your code here

        return new ();
    }
}
