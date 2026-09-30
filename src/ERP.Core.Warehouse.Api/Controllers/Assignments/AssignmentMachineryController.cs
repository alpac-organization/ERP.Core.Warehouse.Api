using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Assignments
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class AssignmentMachineryController(IMediator _mediator) : BaseAssignmentResourceController(_mediator)
    {
        [Tags("Asignaciones operacionales")]
        [HttpGet("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery")]
        [ProducesResponseType(typeof(PagedResponse<GetAssignmentMachineryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<GetAssignmentMachineryDto>> GetAssignmentMachineryAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid operational_order_id,
            [FromRoute] Guid assignment_id,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            var query = new GetAssignmentMachineryQuery
            {
                AssignmentId = assignment_id,
                PageNumber = page_number,
                PageSize = page_size
            };

            return await QueryAsync<PagedResponse<GetAssignmentMachineryDto>, GetAssignmentMachineryQuery>(
                query, Guid.Parse(userIdStr ?? ""), company_id, module_code, operational_order_id);
        }

        [Tags("Asignaciones operacionales")]
        [HttpPost("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignMachinery(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid operational_order_id,
            [FromRoute] Guid assignment_id,
            [FromBody] CreateAssignmentMachineryCommand command)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            command.AssignmentOperationalId = assignment_id;

            return await AssignAsync(command, Guid.Parse(userIdStr ?? ""), company_id, module_code, operational_order_id);
        }

        [Tags("Asignaciones operacionales")]
        [HttpDelete("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery/{assignment_machinery_id}")]
        [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<NoContentResult> DeleteAssignmentMachineryAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid operational_order_id,
            [FromRoute] Guid assignment_id,
            [FromRoute] Guid assignment_machinery_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            var payload = new DeleteAssignmentMachineryCommand
            {
                AssignmentId = assignment_id,
                AssignmentMachineryId = assignment_machinery_id
            };

            return await RemoveAsync(payload, Guid.Parse(userIdStr ?? ""), company_id, module_code, operational_order_id);
        }
    }
}
