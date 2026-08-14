using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;

namespace ERP.Core.Warehouse.Api.Controllers.PurchaseRequests
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class PurchaseRequestController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Solicitudes de compras")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/purchase-requests")]      
        [ProducesResponseType(typeof(CreatedResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<CreatedResult> RegisterPurchaseRequestAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromBody] RegisterPurchaseRequestCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");

            await _mediator.Send(payload);

            return Created();
        }

        [Tags("Solicitudes de compras")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-requests")]      
        [ProducesResponseType(typeof(PagedResponse<PurchaseRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<PagedResponse<PurchaseRequestDto>> GetPurchaseRequestAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] Guid? branch_id = null,
            [FromQuery] PurchaseRequestType? request_type = null,
            [FromQuery] string? code = null,
            [FromQuery] PurchaseRequestStatus? status = null,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size  = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetPurchaseRequestsQuery()
            {
                BranchId = branch_id,
                CompanyId = company_id,
                Code = code,
                Status = status,
                ModuleCode = module_code,
                RequestType = request_type,
                UserId = Guid.Parse(userIdStr ?? ""),
                PageNumber = page_number,
                PageSize = page_size,
            });
        }

        [Tags("Solicitudes de compras")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}")]      
        [ProducesResponseType(typeof(PurchaseRequestDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<PurchaseRequestDetailsDto> GetPurchaseRequestDetailsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_request_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetPurchaseRequestDetailsQuery()
            {
                PurchaseRequestId = purchase_request_id,
                CompanyId = company_id,
                UserId = Guid.Parse(userIdStr ?? ""),
                ModuleCode = module_code
            });
        }

        [Tags("Solicitudes de compras")] 
        [HttpGet("companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}/products")]      
        [ProducesResponseType(typeof(PagedResponse<PurchaseRequestItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<PagedResponse<PurchaseRequestItemDto>> GetPurchaseRequestProductsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_request_id,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size  = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetPurchaseRequestsProductsQuery()
            {
                PurchaseRequestId = purchase_request_id,
                CompanyId = company_id,
                UserId = Guid.Parse(userIdStr ?? ""),
                ModuleCode = module_code,
                PageNumber = page_number,
                PageSize = page_size,
            });
        }


        [Tags("Solicitudes de compras")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}/process")]      
        [ProducesResponseType(typeof(OkResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<OkResult> ProcessPurchaseRequestAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_request_id, [FromBody] ProcessPurchaseRequestCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            payload.PurchaseRequestId = purchase_request_id;

            await _mediator.Send(payload);
            
            return Ok();
        }


        [Tags("Solicitudes de compras")] 
        [HttpDelete("companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}")]      
        [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<NoContentResult> DeletePurchaseRequestAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_request_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            var Payload = new DeletePurchaseRequestCommand()
            {
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                PurchaseRequestId = purchase_request_id
            };

            await _mediator.Send(Payload);
            
            return NoContent();
        }

        [Tags("Solicitudes de compras")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}/send-accounting-review")]      
        [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<NoContentResult> SendPurchaseRequestToReviewAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_request_id, [FromBody] SendPurchaseRequestToReviewCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            payload.PurchaseRequestId = purchase_request_id;
            
            await _mediator.Send(payload);
            
            return NoContent();
        }

        [Tags("Solicitudes de compras")] 
        [HttpPost("companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}/send-management-review")]      
        [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]  
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]  
        public async Task<NoContentResult> SendPurchaseRequestToManagementReviewAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid purchase_request_id, [FromBody] SendPurchaseRequestToManagementReviewCommand payload)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            payload.CompanyId = company_id;
            payload.ModuleCode = module_code;
            payload.UserId = Guid.Parse(userIdStr ?? "");
            payload.PurchaseRequestId = purchase_request_id;
            
            await _mediator.Send(payload);
            
            return NoContent();
        }
    }
}
