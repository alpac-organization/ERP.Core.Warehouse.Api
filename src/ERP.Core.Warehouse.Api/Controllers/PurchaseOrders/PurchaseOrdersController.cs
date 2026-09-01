using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.PurchaseOrders
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class PurchaseOrdersController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Ordenes de compras")]
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-orders")]
        [ProducesResponseType(typeof(PagedResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<PurchaseOrderDto>> GetPurchaseOrdersAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] Guid? area_id = null,
            [FromQuery] Guid? branch_id = null,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetPurchaseOrdersQuery
            {
                CompanyId  = company_id,
                ModuleCode = module_code,
                UserId     = Guid.Parse(userIdStr ?? ""),
                AreaId     = area_id,
                BranchId   = branch_id,
                PageNumber = page_number,
                PageSize   = page_size
            });
        }


        [Tags("Ordenes de compras")]
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-orders/{purchase_order_id}/details")]
        [ProducesResponseType(typeof(PurchaseOrderDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PurchaseOrderDetailsDto> GetPurchaseOrderDetailsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_order_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetPurchaseOrderDetailsQuery
            {
                CompanyId     = company_id,
                ModuleCode    = module_code,
                UserId        = Guid.Parse(userIdStr ?? ""),
                PurchaseOrderId = purchase_order_id
            });
        }


        [Tags("Ordenes de compras")]
        [HttpPost("companies/{company_id}/modules/{module_code}/purchase-orders/{requisition_management_review_id}/process")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<bool> ProcessPurchaseOrderAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid requisition_management_review_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new ProcessPurchaseOrderCommand
            {
                CompanyId                   = company_id,
                ModuleCode                  = module_code,
                UserId                      = Guid.Parse(userIdStr ?? ""),
                RequisitionManagementReviewId = requisition_management_review_id
            });
        }
    }
}
