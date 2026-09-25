using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Machineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Machinery
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class WarehouseMachineriesController(IMediator mediator) : ApiControllerBase
    {
        [Tags("Catálogo de Maquinarias")]
        [HttpGet("companies/{company_id}/modules/{module_code}/machinery")]
        [ProducesResponseType(typeof(IEnumerable<MachineryListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async IEnumerable<MachineryListDto> GetMachineriesAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await mediator.Send(new GetMachineriesQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? "")
            });
        }


        [Tags("Catálogo de Maquinarias")]
        [HttpPost("companies/{company_id}/modules/{module_code}/machinery")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterMachineryAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromBody] MachineryCommand command)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            command.CompanyId = company_id;
            command.ModuleCode = module_code;
            command.UserId = Guid.Parse(userIdStr ?? "");

            return await mediator.Send(Action);
        }
    }
}
