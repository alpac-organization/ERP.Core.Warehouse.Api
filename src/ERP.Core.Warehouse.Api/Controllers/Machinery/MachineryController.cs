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
    public class MachineryController(IMediator mediator) : ApiControllerBase
    {
        [Tags("Catálogo de Maquinarias")]
        [HttpGet("companies/{company_id}/modules/{module_code}/machinery")]
        [ProducesResponseType(typeof(IEnumerable<MachineryListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IEnumerable<MachineryListDto>> GetMachineriesAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code)
        {
            return await mediator.Send(new GetMachineriesQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = CurrentUserId
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
            command.CompanyId = company_id;
            command.ModuleCode = module_code;
            command.UserId = CurrentUserId;

            var result = await mediator.Send(command);
            return Ok(result);
        }
    }
}
