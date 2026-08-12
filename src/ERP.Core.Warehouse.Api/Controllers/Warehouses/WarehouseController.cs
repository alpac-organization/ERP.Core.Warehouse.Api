using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Controllers.Warehouses
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class WarehouseController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Almacenes")]
        [HttpPost("companies/{company_id}/modules/{module_code}/warehouse")]
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<CreatedResult> RegisterWarehouseAsync(
        [FromRoute] Guid company_id,
        [FromRoute] string module_code,
        [FromBody] RegisterWarehouseCommand payload,
        CancellationToken cancellationToken)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            Guid.TryParse(userIdStr, out var userId);

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = userId;

            var response = await _mediator.Send(payload, cancellationToken);

            return Created(string.Empty, response);
        }

        [Tags("Almacenes")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse")]
        [ProducesResponseType(typeof(List<WarehouseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<List<WarehouseDto>> GetWarehouseAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] string? branch_code
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetWarehousesQuery()
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                BranchCode = branch_code
            });
        }
    }
}