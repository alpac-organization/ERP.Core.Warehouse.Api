using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.Reception
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class ReceptionEntranceController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Control de Acceso")]
        [HttpPost("companies/{company_id}/modules/{module_code}/reception-entrances")]
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<CreatedResult> CreateReceptionEntranceAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromBody] CreateReceptionEntranceCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");

            await _mediator.Send(payload);

            return Created();
        }

        [Tags("Control de Acceso")]
        [HttpGet("companies/{company_id}/modules/{module_code}/reception-entrances")]
        [ProducesResponseType(typeof(GetReceptionEntrancesDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<OkResult> GetReceptionEntrancesAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] string? plate_number = null,
            [FromQuery] string? document_number = null,
            [FromQuery] string? contaniner_number = null,
            [FromQuery] DocumentType? document_type = null,

            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            await _mediator.Send(new GetReceptionEntrancesQuery()
            {
                UserId = Guid.Parse(userIdStr ?? ""),
                CompanyId = company_id, 
                ModuleCode = module_code,
                DocumentNumber = document_number,
                PlateNumber = plate_number,
                ContainerNumber = contaniner_number,
                PageSize = page_size,
                PageNumber = page_number,
                DocumentType = document_type
            });

            return Ok();
        }

        [Tags("Control de Acceso")]
        [HttpGet("companies/{company_id}/modules/{module_code}/receptions/{reception_id}")]
        [ProducesResponseType(typeof(ReceptionEntranceDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ReceptionEntranceDetailDto> GetReceptionEntranceDetailAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromRoute] Guid reception_id,
            CancellationToken cancellationToken)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            Guid.TryParse(userIdStr, out var userId);

            return await _mediator.Send(new GetReceptionEntranceDetailQuery
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = userId,
                RecordId = reception_id
            }, cancellationToken);
        }

        [Tags("Control de Acceso")]
        [HttpPost("companies/{company_id}/modules/{module_code}/receptions/{reception_id}/exit")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<OkResult> Exit([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid reception_id, [FromBody] ExitVehicleDto dto
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return Ok();
        }
    }
}
