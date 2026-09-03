using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.WarehouseAssignments
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class WarehouseAssignmentsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public WarehouseAssignmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Tags("Asignacion de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/warehouse-assignment")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignWarehouse(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] CreateWarehouseAssignmentCommand command,
            CancellationToken cancellationToken = default)
        {
            command.CompanyId = companyId;
            command.ModuleCode = moduleCode;
            command.ReceptionId = receptionId;
            command.UserId = CurrentUserId;

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignacion de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/unloading-crew")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignUnloadingCrew(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] CreateUnloadingCrewCommand command,
            CancellationToken cancellationToken = default)
        {
            command.CompanyId = companyId;
            command.ModuleCode = moduleCode;
            command.ReceptionId = receptionId;
            command.UserId = CurrentUserId;

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignacion de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/unloading-machinery")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignUnloadingMachinery(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] CreateUnloadingMachineryCommand command,
            CancellationToken cancellationToken = default)
        {
            command.CompanyId = companyId;
            command.ModuleCode = moduleCode;
            command.ReceptionId = receptionId;
            command.UserId = CurrentUserId;

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignacion de Bodega")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments/pending")]
        [ProducesResponseType(typeof(PagedResponse<PendingWarehouseAssignmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPending(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromQuery(Name = "driver_name")] string? driverName,
            [FromQuery(Name = "license_plate")] string? licensePlate,
            [FromQuery(Name = "document_type")] DocumentType? documentType,
            [FromQuery(Name = "service_order_code")] string? serviceOrderCode,
            [FromQuery(Name = "page_number")] int pageNumber = 1,
            [FromQuery(Name = "page_size")] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPendingWarehouseAssignmentsQuery
            {
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = CurrentUserId,
                DriverName = driverName,
                LicensePlate = licensePlate,
                DocumentType = documentType,
                ServiceOrderCode = serviceOrderCode,
                PageNumber = pageNumber > 0 ? pageNumber : 1,
                PageSize = pageSize > 0 ? pageSize : 10
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }


        [Tags("Asignacion de Bodega")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments/{reception_id}")]
        [ProducesResponseType(typeof(WarehouseAssignmentDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAssignmentById(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromQuery(Name = "entrance_ducat_id")] Guid? entranceDucatId,
            CancellationToken cancellationToken = default)
        {
            var query = new GetWarehouseAssignmentByIdQuery 
            { 
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = CurrentUserId,
                ReceptionId = receptionId,
                EntranceDucatId = entranceDucatId 
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignacion de Bodega")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments")]
        [ProducesResponseType(typeof(PagedResponse<WarehouseAssignmentDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAssignmentsHistory(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromQuery(Name = "driver_name")] string? driverName,
            [FromQuery(Name = "license_plate")] string? licensePlate,
            [FromQuery(Name = "document_type")] DocumentType? documentType,
            [FromQuery(Name = "service_order_code")] string? serviceOrderCode,
            [FromQuery(Name = "page_number")] int pageNumber = 1,
            [FromQuery(Name = "page_size")] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = new GetWarehouseAssignmentsHistoryQuery
            {
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = CurrentUserId,
                DriverName = driverName,
                LicensePlate = licensePlate,
                DocumentType = documentType,
                ServiceOrderCode = serviceOrderCode,
                PageNumber = pageNumber > 0 ? pageNumber : 1,
                PageSize = pageSize > 0 ? pageSize : 10
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignacion de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/complete-assignment")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompleteAssignment(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] CompleteWarehouseAssignmentCommand command,
            CancellationToken cancellationToken = default)
        {
            command.CompanyId = companyId;
            command.ModuleCode = moduleCode;
            command.ReceptionId = receptionId;
            command.UserId = CurrentUserId;

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
