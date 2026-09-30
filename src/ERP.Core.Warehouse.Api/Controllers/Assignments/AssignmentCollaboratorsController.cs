using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Controllers.Assignments
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class AssignmentCollaboratorsController(IMediator mediator) : ApiControllerBase
    {
        [Tags("Asignaciones operacionales")]
        [HttpGet("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/collaborators")]
        [ProducesResponseType(typeof(PagedResponse<GetAssignmentCollaboratorsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<GetAssignmentCollaboratorsDto>> GetAssignmentCollaboratorsAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid operational_order_id,
            [FromRoute] Guid assignment_id,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await mediator.Send(new GetAssignmentCollaboratorsQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                OperationalOrderId = operational_order_id,
                AssignmentId = assignment_id,
                PageNumber = page_number,
                PageSize = page_size
            });
        }

        [Tags("Asignaciones operacionales")]
        [HttpPost("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/collaborators")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignCollaborators(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid operational_order_id,
            [FromRoute] Guid assignment_id,
            [FromBody] CreateAssignmentCollaboratorsCommand command)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            command.CompanyId = company_id;
            command.ModuleCode = module_code;
            command.OperationalOrderId = operational_order_id;
            command.AssignmentOperationalId = assignment_id;
            command.UserId = Guid.Parse(userIdStr ?? "");

            var result = await mediator.Send(command);

            return Ok(result);
        }
    }
}
