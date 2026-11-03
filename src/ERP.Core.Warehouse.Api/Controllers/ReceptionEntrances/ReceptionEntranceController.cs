using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;

using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;

namespace ERP.Core.Warehouse.Api.Controllers.ReceptionEntrances
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
        [ProducesResponseType(typeof(ReceptionEntranceListResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ReceptionEntranceListResponseDto> GetReceptionEntrancesAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] bool only_day = true,
            [FromQuery] DateTime? date = null,
            [FromQuery] DateTime? filter_date = null,
            [FromQuery] DateTime? entry_date = null,
            [FromQuery] DateTime? reception_date = null,
            [FromQuery] DateTime? created_at = null,
            [FromQuery] string? plate_number = null,
            [FromQuery] string? document_number = null,
            [FromQuery] string? container_number = null,
            [FromQuery] string? contaniner_number = null,
            [FromQuery] string? reception_code = null,
            [FromQuery] string? code = null,
            [FromQuery] DocumentType? document_type = null,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            var requestedDate = date ?? filter_date ?? entry_date ?? reception_date ?? created_at;

            return await _mediator.Send(new GetReceptionEntrancesQuery()
            {
                UserId = Guid.Parse(userIdStr ?? ""),
                CompanyId = company_id,
                OnlyDay = only_day,
                Date = requestedDate.HasValue ? DateOnly.FromDateTime(requestedDate.Value) : (DateOnly?)null,
                ModuleCode = module_code,
                DocumentNumber = document_number,
                PlateNumber = plate_number,
                ContainerNumber = container_number ?? contaniner_number,
                ReceptionCode = reception_code ?? code,
                PageSize = page_size,
                PageNumber = page_number,
                DocumentType = document_type
            });
        }

        [Tags("Control de Acceso")]
        [HttpGet("companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details")]
        [ProducesResponseType(typeof(ReceptionEntranceDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ReceptionEntranceDetailsDto> GetReceptionEntranceDetailAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid reception_id, [FromRoute] Guid reception_entrance_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            return await _mediator.Send(new GetReceptionEntranceDetailsQuery()
            {
                ModuleCode = module_code,
                CompanyId = company_id,
                UserId = Guid.Parse(userIdStr ?? ""),
                ReceptionEntranceId = reception_entrance_id,
            });
        }

        [Tags("Control de Acceso")]
        [HttpPatch("companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}")]
        [ProducesResponseType(typeof(ReceptionEntranceDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<OkResult> UpdateReceptionEntranceAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid reception_id, [FromRoute] Guid reception_entrance_id, [FromBody] UpdateReceptionEntranceCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            payload.ReceptionEntranceId = reception_entrance_id;
            await _mediator.Send(payload);
            return Ok();
        }
    }
}