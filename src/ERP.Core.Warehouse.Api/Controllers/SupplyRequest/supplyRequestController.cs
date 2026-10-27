using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Errors;

using ERP.Core.Infrastructure.Attributes;

using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.SupplyRequests.v1.Commands;


namespace ERP.Core.Warehouse.Api.Controllers.supplyRequest;

[HasToken]
[ApiVersion("1.0")]
[Route("api/v1/")]
[Tags("Solicitudes Operacionales de Inventario")]
public class SupplyRequestsController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("companies/{company_id}/modules/{module_code}/supply-requests")]
    [ProducesResponseType(typeof(PagedResponse<GetSupplyRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<PagedResponse<GetSupplyRequestDto>> GetSupplyRequestsAsync(
        [FromRoute] Guid        company_id,
        [FromRoute] string      module_code,
        [FromQuery] string?     code,
        [FromQuery] Guid?       work_area_id,
        [FromQuery] Guid?       cost_center_id,
        [FromQuery] Guid?       branch_id,
        [FromQuery] Guid?       warehouse_id,
        [FromQuery] Guid?       operational_order_id,
        [FromQuery] Guid?       requested_by_user_id,
        [FromQuery] int         page_number = 1,
        [FromQuery] int         page_size = 10
    )
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetSupplyRequestsQuery()
        {
            CompanyId           = company_id,
            ModuleCode          = module_code,
            UserId              = Guid.Parse(userIdStr ?? ""),
            Code                = code,
            WorkAreaId          = work_area_id,
            CostCenterId        = cost_center_id,
            BranchId            = branch_id,
            WarehouseId         = warehouse_id,
            OperationalOrderId  = operational_order_id,
            RequestedByUserId   = requested_by_user_id,
            PageNumber          = page_number,
            PageSize            = page_size
        });
    }

    [HttpGet("companies/{company_id}/modules/{module_code}/supply-requests/{supply_request_id}/detail")]
    [ProducesResponseType(typeof(GetSupplyRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<GetSupplyRequestDetailDto> GetSupplyRequestsDetailAsync(
        [FromRoute] Guid        company_id,
        [FromRoute] string      module_code,
        [FromRoute] Guid        supply_request_id
    )
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        return await mediator.Send(new GetSupplyRequestsDetailQuery()
        {
            CompanyId           = company_id,
            ModuleCode          = module_code,
            UserId              = Guid.Parse(userIdStr ?? ""),
            SupplyRequestId     = supply_request_id
        });
    }

    [HttpPost("companies/{company_id}/modules/{module_code}/supply-requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> RegisterSupplyRequest(
        [FromRoute] Guid                            company_id,
        [FromRoute] string                          module_code,
        [FromBody] RegisterSupplyRequestsCommad     command)
    {
        var userIdStr = HttpContext.Items["UserId"] as string;

        command.CompanyId   = company_id;
        command.ModuleCode  = module_code;
        command.UserId      = Guid.Parse(userIdStr ?? "");

        await mediator.Send(command);

        return Created();
    }
}