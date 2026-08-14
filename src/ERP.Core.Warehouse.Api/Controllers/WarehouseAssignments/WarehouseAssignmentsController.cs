using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.WarehouseAssignments;

[ApiVersion("1.0")]
[Route("api/v1/")]
public class WarehouseAssignmentsController(IMediator _mediator) : ApiControllerBase
{
    [Tags("Asignación de Bodega")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments/pending")]
    [ProducesResponseType(typeof(PagedWarehouseAssignmentsDto<PendingDocumentItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedWarehouseAssignmentsDto<PendingDocumentItemDto>> GetPendingAssignmentsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? driver_name,
        [FromQuery] string? plate_number,
        [FromQuery] DocumentType? document_type,
        [FromQuery] string? document_number,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetPendingAssignmentsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            DriverName = driver_name,
            PlateNumber = plate_number,
            DocumentType = document_type,
            DocumentNumber = document_number,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }

    [Tags("Asignación de Bodega")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments")]
    [ProducesResponseType(typeof(PagedWarehouseAssignmentsDto<WarehouseAssignmentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedWarehouseAssignmentsDto<WarehouseAssignmentListItemDto>> GetWarehouseAssignmentsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] string? driver_name,
        [FromQuery] string? plate_number,
        [FromQuery] int page_number = 1,
        [FromQuery] int page_size = 10,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetWarehouseAssignmentsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            DriverName = driver_name,
            PlateNumber = plate_number,
            PageNumber = page_number,
            PageSize = page_size
        }, cancellationToken);
    }

    [Tags("Asignación de Bodega")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments/available-warehouses")]
    [ProducesResponseType(typeof(List<AvailableWarehouseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<AvailableWarehouseDto>> GetAvailableWarehousesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromQuery] DocumentType? document_type,
        [FromQuery] Guid? rack_id,
        [FromQuery] Guid? lot_id,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetAvailableWarehousesQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            DocumentType = document_type,
            RackId = rack_id,
            LotId = lot_id
        }, cancellationToken);
    }

    [Tags("Asignación de Bodega")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments/documents/{document_id}")]
    [ProducesResponseType(typeof(WarehouseAssignmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<WarehouseAssignmentDetailDto> GetWarehouseAssignmentDetailAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid document_id,
        [FromQuery] DocumentType document_type,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetWarehouseAssignmentDetailQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId,
            DocumentId = document_id,
            DocumentType = document_type
        }, cancellationToken);
    }

    [Tags("Asignación de Bodega")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-machineries")]
    [ProducesResponseType(typeof(List<WarehouseMachineryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<WarehouseMachineryDto>> GetWarehouseMachineriesAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetWarehouseMachineriesQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId
        }, cancellationToken);
    }

    [Tags("Asignación de Bodega")]
    [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-staffs")]
    [ProducesResponseType(typeof(List<WarehouseStaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<List<WarehouseStaffDto>> GetWarehouseStaffsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        return await _mediator.Send(new GetWarehouseStaffsQuery
        {
            CompanyId = company_id,
            ModuleCode = module_code,
            UserId = userId
        }, cancellationToken);
    }

    [Tags("Asignación de Bodega")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-assignments/documents/{document_id}/assignment")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CreateWarehouseAssignmentAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid document_id,
        [FromQuery] DocumentType document_type,
        [FromBody] CreateWarehouseAssignmentCommand command,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        command.DocumentId = document_id;
        command.DocumentType = document_type;
        command.UserId = userId;
        command.CompanyId = company_id;
        command.ModuleCode = module_code;

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Tags("Asignación de Bodega")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-assignments/documents/{document_id}/unloading-details")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CreateUnloadingDetailsAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid document_id,
        [FromQuery] DocumentType document_type,
        [FromBody] CreateUnloadingDetailsCommand command,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        command.DocumentId = document_id;
        command.DocumentType = document_type;
        command.UserId = userId;
        command.CompanyId = company_id;
        command.ModuleCode = module_code;

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Tags("Asignación de Bodega")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-assignments/documents/{document_id}/unloading-crew")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CreateUnloadingCrewAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid document_id,
        [FromQuery] DocumentType document_type,
        [FromBody] CreateUnloadingCrewCommand command,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        command.DocumentId = document_id;
        command.DocumentType = document_type;
        command.UserId = userId;
        command.CompanyId = company_id;
        command.ModuleCode = module_code;

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Tags("Asignación de Bodega")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-assignments/documents/{document_id}/unloading-machinery")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CreateUnloadingMachineryAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid document_id,
        [FromQuery] DocumentType document_type,
        [FromBody] CreateUnloadingMachineryCommand command,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        command.DocumentId = document_id;
        command.DocumentType = document_type;
        command.UserId = userId;
        command.CompanyId = company_id;
        command.ModuleCode = module_code;

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    [Tags("Asignación de Bodega")]
    [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-assignments/documents/{document_id}/complete-assignment")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<OkObjectResult> CompleteWarehouseAssignmentAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromRoute] Guid document_id,
        [FromQuery] DocumentType document_type,
        CancellationToken cancellationToken)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;
        Guid.TryParse(userIdStr, out var userId);

        var command = new CompleteWarehouseAssignmentCommand
        {
            DocumentId = document_id,
            DocumentType = document_type,
            UserId = userId,
            CompanyId = company_id,
            ModuleCode = module_code
        };

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }
}
