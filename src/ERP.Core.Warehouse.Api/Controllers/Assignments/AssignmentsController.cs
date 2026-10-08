using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;

namespace ERP.Core.Warehouse.Api.Controllers.Assignments
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    [Tags("Asignaciones Operativas")]
    public class AssignmentsController(IMediator _mediator) : ApiControllerBase
    {
        [HttpGet("companies/{company_id}/modules/{module_code}/assignments")]
        [ProducesResponseType(typeof(PagedResponse<AssignmentOperationalDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<AssignmentOperationalDto>> GetAssignmentsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromQuery] Guid? operational_order_id = null, 
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size   = 10,
            [FromQuery] AssignmentOperationalStatus? status = null
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetAssignmentsQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                OperationalOrderId = operational_order_id,
                Status = status,
                PageNumber = page_number,
                PageSize = page_size,
                UserId = Guid.Parse(userIdStr ?? "")
            });
        }

        [HttpPost("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments")]
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<CreatedResult> CreateAssignmentAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid operational_order_id, [FromBody] CreateAssignmentCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.OperationalOrderId = operational_order_id;
            payload.UserId = Guid.Parse(userIdStr ?? "");

            await _mediator.Send(payload);

            return Created();
        }

        [HttpGet("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/details")]
        [ProducesResponseType(typeof(AssignmentOperationalDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<AssignmentOperationalDetailsDto> GetAssignmentDetailsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid operational_order_id, [FromRoute] Guid assignment_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetAssignmentDetailsQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                AssignmentId = assignment_id,
                UserId = Guid.Parse(userIdStr ?? "")
            });
        }

        [HttpPatch("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}")]
        [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<NoContentResult> UpdateAssignmentAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid operational_order_id, [FromRoute] Guid assignment_id, [FromBody] UpdateAssignmentCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.AssignmentId = assignment_id;
            payload.OperationalOrderId = operational_order_id;
            payload.UserId = Guid.Parse(userIdStr ?? "");

            await _mediator.Send(payload);

            return NoContent();
        }

        [HttpDelete("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}")]
        [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<NoContentResult> DeleteAssignmentAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid operational_order_id, [FromRoute] Guid assignment_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            var payload = new DeleteAssignmentCommand
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                AssignmentId = assignment_id,
                OperationalOrderId = operational_order_id,
                UserId = Guid.Parse(userIdStr ?? "")
            };

            await _mediator.Send(payload);

            return NoContent();
        }

        [HttpGet("companies/{company_id}/modules/{module_code}/assignments")]
        [ProducesResponseType(typeof(MerchandiseLocationDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<MerchandiseLocationDetailsDto> GetMerchandiseLocationDetailsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromQuery] string assignment_code)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetMerchandiseLocationDetailsQuery()
            {
                AssignmentCode = assignment_code,
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? "")
            });
        }
    }
}