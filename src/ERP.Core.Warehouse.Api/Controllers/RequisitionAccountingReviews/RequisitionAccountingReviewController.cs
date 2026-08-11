using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.RequisitionAccountingReviews
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class RequisitionAccountingReviewController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Revisiones contables")]
        [HttpGet("companies/{company_id}/modules/{module_code}/requisition-accounting-reviews")]
        [ProducesResponseType(typeof(PagedResponse<RequisitionAccountingReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<RequisitionAccountingReviewDto>> GetRequisitionAccountingReviewsAsync([FromRoute] Guid company_id, [FromRoute] string module_code,
            [FromQuery] AccountingReviewStatus? status = null,
            [FromQuery] int page_number = 1,
            [FromQuery] int page_size = 10
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetRequisitionAccountingReviewsQuery()
            {
                Status = status,
                CompanyId = company_id,
                ModuleCode = module_code,
                UserId = Guid.Parse(userIdStr ?? ""),
                PageNumber = page_number,
                PageSize = page_size,
            });
        }

        [Tags("Revisiones contables")]
        [HttpGet("companies/{company_id}/modules/{module_code}/requisition-accounting-reviews/{requisition_accounting_review_id}")]
        [ProducesResponseType(typeof(RequisitionAccountingReviewDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<RequisitionAccountingReviewDetailsDto> GetRequisitionAccountingReviewDetailsAsync([FromRoute] Guid company_id, [FromRoute] string module_code, [FromRoute] Guid requisition_accounting_review_id)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetRequisitionAccountingReviewDetailsQuery()
            {
                RequisitionAccountingReviewId = requisition_accounting_review_id,
                CompanyId = company_id,
                UserId = Guid.Parse(userIdStr ?? ""),
                ModuleCode = module_code
            });
        }
    }
}
