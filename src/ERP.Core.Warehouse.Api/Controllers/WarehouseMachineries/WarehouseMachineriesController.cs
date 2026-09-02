using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.WarehouseMachineries
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class WarehouseMachineriesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public WarehouseMachineriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Tags("Maquinarias de Bodega")]
        [HttpPost("companies/{company_id}/modules/{module_code}/warehouse-machineries")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateWarehouseMachinery(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            [FromBody] CreateWarehouseMachineryCommand command,
            CancellationToken cancellationToken = default)
        {
            command.CompanyId = companyId;
            command.ModuleCode = moduleCode;
            command.UserId = CurrentUserId;

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Tags("Maquinarias de Bodega")]
        [HttpGet("companies/{company_id}/modules/{module_code}/warehouse-machineries")]
        [ProducesResponseType(typeof(IEnumerable<WarehouseMachineryListDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWarehouseMachineries(
            [FromRoute(Name = "company_id")] Guid companyId,
            [FromRoute(Name = "module_code")] string moduleCode,
            CancellationToken cancellationToken = default)
        {
            var query = new GetWarehouseMachineriesQuery 
            { 
                CompanyId = companyId,
                ModuleCode = moduleCode,
                UserId = CurrentUserId
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }
}
