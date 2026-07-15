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
        public async Task<CreatedResult> RegisterWarehouseAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromBody] RegisterWarehouseCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;            
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            
            await _mediator.Send(payload);

            return Created();
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