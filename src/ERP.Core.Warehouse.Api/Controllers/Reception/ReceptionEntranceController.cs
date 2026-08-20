using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Reception;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
public class ReceptionEntranceController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Control de Acceso")]
    [HttpPost("companies/{company_id}/modules/{module_code}/reception-entrances")]
    [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<CreatedResult> CreateReceptionEntranceAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] CreateReceptionEntranceDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToCommand(
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Created(string.Empty, null);
    }

    [Tags("Control de Acceso")]
    [HttpGet("companies/{company_id}/modules/{module_code}/reception-entrances")]
    [ProducesResponseType(typeof(GetReceptionEntrancesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetReceptionEntrancesDto> GetReceptionEntrancesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? driver_name,
        [FromQuery] string? plate_number,
        [FromQuery] DocumentType? document_type,
        [FromQuery] string? document_number,
        [FromQuery] string? ducat_number,
        [FromQuery] Guid? ducat_id,
        [FromQuery] DateTime? start_date,
        [FromQuery] DateTime? end_date,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetReceptionEntrancesQuery()
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            DriverName = driver_name,
            PlateNumber = plate_number,
            DocumentType = document_type,
            DocumentNumber = document_number,
            DucatNumber = ducat_number,
            DucatId = ducat_id,
            StartDate = start_date,
            EndDate = end_date,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }

    [Tags("Control de Acceso")]
    [HttpGet("companies/{company_id}/modules/{module_code}/receptions/{reception_id}")]
    [ProducesResponseType(typeof(ReceptionEntranceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ReceptionEntranceDetailDto> GetReceptionEntranceDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetReceptionEntranceDetailQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            RecordId = reception_id
        }, cancellationToken);
    }


    [Tags("Control de Acceso")]
    [HttpPatch("companies/{company_id}/modules/{module_code}/receptions/{reception_id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> UpdateReceptionEntranceAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        [FromBody] UpdateReceptionEntranceDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToUpdateCommand(
            receptionId: reception_id,
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }


    [Tags("Control de Acceso")]
    [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/ducats")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> AddDucatsToReceptionAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        [FromBody] AddDucatsToReceptionDto dto,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToAddDucatsCommand(
            receptionId: reception_id,
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    [Tags("Control de Acceso")]
    [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/exit")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> RegisterVehicleExitAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid reception_id,
    [FromBody] ExitVehicleDto dto,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = dto.ToExitVehicleCommand(
            receptionId: reception_id,
            userId: userId,
            companyId: company_id,
            moduleCode: module_code
        );

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    [Tags("Control de Acceso")]
    [HttpDelete("companies/{company_id}/modules/{module_code}/receptions/{reception_id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> DeleteReceptionEntranceAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromRoute] Guid reception_id,
    CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = new DeleteReceptionEntranceCommand
        {
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code,
            ReceptionId = reception_id
        };

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    [Tags("Control de Acceso")]
    [HttpGet("companies/{company_id}/modules/{module_code}/receptions/deleted-evidences")]
    [ProducesResponseType(typeof(GetDeletedEvidencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetDeletedEvidencesDto> GetDeletedEvidencesAsync(
    [FromRoute] Guid company_id,
    [FromRoute] string module_code,
    [FromQuery] int page_number = 1,
    [FromQuery] int page_size = 10,
    CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetDeletedEvidencesQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }

    [Tags("Control de Acceso")]
    [HttpDelete("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/evidence/permanent")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> PermanentDeleteEvidenceAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid reception_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = new PermanentDeleteEvidenceCommand
        {
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code,
            ReceptionId = reception_id
        };

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }
}
