using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
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

        [Tags("Asignación de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/warehouse-assignment")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignWarehouse(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] AssignWarehouseDto dto,
            CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var command = dto.ToCommand(receptionId, userId, companyId, moduleCode);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignación de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/unloading-crew")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignUnloadingCrew(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] AssignUnloadingCrewDto dto,
            CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var command = dto.ToCommand(receptionId, userId, companyId, moduleCode);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignación de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/unloading-machinery")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignUnloadingMachinery(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] AssignUnloadingMachineryDto dto,
            CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var command = dto.ToCommand(receptionId, userId, companyId, moduleCode);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignación de Bodega")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments/pending")]
        [ProducesResponseType(typeof(IEnumerable<PendingWarehouseAssignmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPending(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var query = new GetPendingWarehouseAssignmentsQuery
            {
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignación de Bodega")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-staffs")]
        [ProducesResponseType(typeof(IEnumerable<WarehouseStaffDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWarehouseStaffs(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var query = new GetWarehouseStaffsQuery
            {
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignación de Bodega")]
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
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var query = new GetWarehouseAssignmentByIdQuery 
            { 
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = userId,
                ReceptionId = receptionId,
                EntranceDucatId = entranceDucatId 
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignación de Bodega")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-assignments")]
        [ProducesResponseType(typeof(IEnumerable<WarehouseAssignmentDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAssignmentsHistory(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var query = new GetWarehouseAssignmentsHistoryQuery
            {
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = userId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [Tags("Asignación de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/complete-assignment")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompleteAssignment(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "reception_id")] Guid receptionId,
            [FromBody] CompleteWarehouseAssignmentDto dto,
            CancellationToken cancellationToken = default)
        {
            var userId = Guid.Parse(HttpContext.Items["UserId"] as string ?? Guid.Empty.ToString());
            var command = new CompleteWarehouseAssignmentCommand
            {
                ReceptionId = receptionId,
                EntranceDucatId = dto.EntranceDucatId,
                UserId = userId,
                CompanyId = companyId,
                ModuleCode = moduleCode
            };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
