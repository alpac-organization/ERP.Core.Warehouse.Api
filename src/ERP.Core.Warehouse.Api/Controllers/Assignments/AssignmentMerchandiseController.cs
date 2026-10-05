using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Assignments
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class AssignmentMerchandiseController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Asignaciones mercancias")]
        [HttpPost("companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/merchandise")]
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<AssignMerchandiseDesignatedLocationDto> AssignMerchandiseDesignatedLocationAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid operational_order_id, [FromRoute] Guid assignment_id, 
            [FromBody] AssignMerchandiseDesignatedLocationCommand payload
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            payload.AssignmentOperationalId = assignment_id;
            payload.OperationalOrderId = operational_order_id;

            return await _mediator.Send(payload);
        }
    }
}
