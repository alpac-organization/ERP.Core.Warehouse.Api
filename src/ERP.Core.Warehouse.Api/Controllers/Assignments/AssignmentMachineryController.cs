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
    public class AssignmentMachineryController(IMediator mediator) : ApiControllerBase
    {
        [Tags("Asignaciones operacionales")]
        [HttpGet("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery")]
        [ProducesResponseType(typeof(PagedResponse<GetAssignmentMachineryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<GetAssignmentMachineryDto>> GetAssignmentMachineryAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid assignment_id,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await mediator.Send(new GetAssignmentMachineryQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                AssignmentId = assignment_id,
                PageNumber = page_number,
                PageSize = page_size                
            });
            
        }

        [Tags("Asignaciones operacionales")]
        [HttpPost("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AssignMachinery(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromRoute(Name = "operational_order_id")] Guid operationalOrderId,
            [FromRoute(Name = "assignment_id")] Guid assignmentId,
            [FromBody] CreateAssignmentMachineryCommand command,
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
