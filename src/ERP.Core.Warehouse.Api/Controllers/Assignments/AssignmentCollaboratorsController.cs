using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;

namespace ERP.Core.Warehouse.Api.Controllers.Assignments
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class AssignmentCollaboratorsController(IMediator mediator) : ApiControllerBase
    {
        [Tags("Asignaciones operacionales")]
        [HttpPost("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/collaborators")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignCollaborators(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "operational_order_id")] Guid operationalOrderId,
            [FromRoute(Name = "assignment_id")] Guid assignmentId,
            [FromBody] CreateAssignmentCollaboratorsCommand command,
            CancellationToken cancellationToken = default)
        {
            command.CompanyId = companyId;
            command.ModuleCode = moduleCode;
            command.OperationalOrderId = operationalOrderId;
            command.AssignmentOperationalId = assignmentId;
            command.UserId = CurrentUserId;

            var result = await mediator.Send(command, cancellationToken);

            return Ok(result);
        }
    }
}
