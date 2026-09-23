using MediatR;
using Microsoft.AspNetCore.Mvc;
using ERP.Core.Domain.Entities.Errors;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Queries;

namespace ERP.Core.Warehouse.Api.Controllers.CustomBranches
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class CustomBranchesController(IMediator _mediator) : ApiControllerBase
    {
        [Tags("Aduanas")]
        [HttpGet("companies/{company_id}/modules/{module_code}/customs-branches")]
        [ProducesResponseType(typeof(PagedResponse<CustomsBranchDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<PagedResponse<CustomsBranchDto>> GetCustomsBranchesAsync(
            [FromRoute] Guid company_id,
            [FromRoute] string module_code,
            [FromQuery] int page_size = 10,
            [FromQuery] int page_number = 1
        )
        {
            var userIdStr = HttpContext.Items["UserId"] as string;

            return await _mediator.Send(new GetCustomBranchesQuery
            {
                UserId = Guid.Parse(userIdStr ?? ""),
                CompanyId = company_id,
                ModuleCode = module_code,
                PageSize = page_size,
                PageNumber = page_number
            });
        }
    }
}
