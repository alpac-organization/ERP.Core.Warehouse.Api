using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;

using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.MerchandiseRegistry.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.MerchandiseRegistry;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class MerchandiseRegistryControlle(IMediator _mediator) : ApiControllerBase
{
    [Tags("Registro de Mercadería")]
    [HttpGet("companies/{company_id}/modules/{module_code}/merchandise-registry")]
    [ProducesResponseType(typeof(GetMerchandiseRegistryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetMerchandiseRegistryDto> GetMerchandiseRegistryAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DateTime? start_date,
        [FromQuery] DateTime? end_date,
        [FromQuery] string? driver_name,
        [FromQuery] string? vehicle_plate_number,
        [FromQuery] DocumentType? document_type,
        [FromQuery] string? document_number,
        [FromQuery] string? ducat_number,
        [FromQuery] string? service_order_code,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetMerchandiseRegistryQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            StarDate = start_date,
            EndDate = end_date,
            DriverName = driver_name,
            VehiclePlateNumber = vehicle_plate_number,
            DocumentType = document_type,
            DocumentNumber = document_number,
            DucatNumber = ducat_number,
            ServiceOrderCode = service_order_code,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }

    [Tags("Registro de Mercadería")]
    [HttpGet("companies/{company_id}/modules/{module_code}/merchandise-registry/{reception_id}")]
    [ProducesResponseType(typeof(GetMerchandiseRegistryDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetMerchandiseRegistryDetailDto> GetMerchandiseRegistryDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetMerchandiseRegistryDetailsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            ReceptionId = reception_id,
            UserId = userId
        }, cancellationToken);
    }

    [Tags("Registro de Mercadería")]
    [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/ducat-registry")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CreateDucatRegistryAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        [FromBody] CreateDucatRegistryDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToCommand(
             receptionId: reception_id,
             userId: userId,
             companyId: company_id,
             moduleCode: module_code
         );

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Tags("Registro de Mercadería")]
    [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/ducats/{ducat_id}/detail")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CreateDucatRegistryDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        [FromRoute] Guid ducat_id,
        [FromBody] CreateDucatRegistryDetailDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToCommand(
            receptionId: reception_id,
            entranceDucatId: ducat_id,
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    [Tags("Registro de Mercadería")]
    [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/customs-declaration/service-order")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> AssignServiceOrderToCustomsDeclarationAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid reception_id,
    [FromBody] AssignServiceOrderToCustomsDeclarationDto dto,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = new AssignServiceOrderToCustomsDeclarationCommand
        {
            ReceptionId = reception_id,
            ServiceOrderId = dto.ServiceOrderId,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    #region Merchandise
    [Tags("Catálogo de Mercadería")]
    [HttpGet("companies/{company_id}/modules/{module_code}/merchandises")]
    [ProducesResponseType(typeof(List<MerchandiseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<MerchandiseDto>> GetMerchandisesAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromQuery] Guid? category_id,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetMerchandisesQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            CategoryProductId = category_id
        }, cancellationToken);
    }

    [Tags("Catálogo de Mercadería")]
    [HttpPost("companies/{company_id}/modules/{module_code}/merchandises")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<Guid> RegisterMerchandiseAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] RegisterMerchandiseDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToCommand(
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        return await _mediator.Send(command, cancellationToken);
    }
    #endregion
}