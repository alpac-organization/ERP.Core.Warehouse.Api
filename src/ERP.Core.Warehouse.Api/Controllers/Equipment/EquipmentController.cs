using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;

using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Equipment
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    [Tags("Catálogo de Equipos")]
    public class EquipmentController(IMediator mediator) : ApiControllerBase
    {
        [HttpGet("companies/{company_id}/modules/{module_code}/equipment")]
        [ProducesResponseType(typeof(PagedResponse<EquipmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<EquipmentDto>> GetEquipmentsAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] EquipmentType? type,
            [FromQuery] Guid? branch_id = null,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await mediator.Send(new GetEquipmentsQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                Type = type,
                BranchId = branch_id,
                PageNumber = page_number,
                PageSize = page_size
            });
        }

        [HttpPost("companies/{company_id}/modules/{module_code}/equipment")]
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<CreatedResult> RegisterEquipmentAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromBody] EquipmentCommand command)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            command.CompanyId = company_id;
            command.ModuleCode = module_code;
            command.UserId = Guid.Parse(userIdStr ?? "");

            await mediator.Send(command);

            return Created();
        }

        [HttpPatch("companies/{company_id}/modules/{module_code}/equipment/{equipment_id}")]
        [ProducesResponseType(typeof(OkResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<OkResult> PatchEquipmentAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid equipment_id, [FromBody] PatchEquipmentCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.UserId = Guid.Parse(userIdStr ?? "");
            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.EquipmentId = equipment_id;

            await mediator.Send(payload);

            return Ok();
        }
    }
}