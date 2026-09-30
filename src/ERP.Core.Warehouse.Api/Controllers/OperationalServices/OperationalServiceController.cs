using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.OperationalServices.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.OperationalServices;

[HasToken]
[ApiVersion("1.0")]
[Route("/api/v1/")]
public class OperationalServicesController(IMediator mediator) : ApiControllerBase
{
    [Tags("Catálogo de servicios operativos")]
    [HttpGet("companies/{company_id}/modules/{module_code}/operational-services")]
    [ProducesResponseType(typeof(PagedResponse<GetOperationalServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<GetOperationalServiceDto>> GetOperationalServiceAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? code,
        [FromQuery] string? name,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetOperationalServicesQuery
        {
            CompanyId       = company_id,
            ModuleCode      = module_code,
            UserId          = Guid.Parse(userIdStr ?? ""),
            ServiceCode     = code,
            ServiceName     = name,
            PageNumber      = page_number,
            PageSize        = page_size
        });
    }

    [Tags("Catálogo de servicios operativos")]
    [HttpPost("companies/{company_id}/modules/{module_code}/operational-services")]
    [ProducesResponseType(typeof(Unit), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterOperationalServicesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] RegisterOperationalServicesCommand command)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        command.CompanyId = company_id;
        command.ModuleCode = module_code;
        command.UserId = Guid.Parse(userIdStr ?? "");

        await mediator.Send(command);
        
        return Ok();
    }
}