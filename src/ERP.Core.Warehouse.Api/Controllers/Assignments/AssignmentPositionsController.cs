using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Controllers.Assignments
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class AssignmentPositionsController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Asignaciones de posiciones")]
        [HttpPatch("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/assignment-resources")]
        [ProducesResponseType(typeof(AssignMerchandiseDesignatedLocationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAssignmentPositionsAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid operational_order_id,
            [FromRoute] Guid assignment_id,
            [FromBody] AssignMerchandiseDesignatedLocationCommand payload
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            payload.AssignmentOperationalId = assignment_id;
            payload.OperationalOrderId = operational_order_id;

            var result = await _mediator.Send(payload);

            if (string.IsNullOrWhiteSpace(result.CodeQr) && string.IsNullOrWhiteSpace(result.CodeBar))
            {
                return NoContent();
            }

            return Ok(result);
        }
    }
}