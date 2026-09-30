using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

using ERP.Core.Domain.Entities.Bases;

using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Controllers.ApiBase
{
    public abstract class BaseAssignmentResourceController(IMediator mediator) : ApiControllerBase
    {
        protected async Task<TResponse> QueryAsync<TResponse, TRequest>(
            TRequest request,
            Guid userId,
            Guid companyId,
            string moduleCode,
            Guid operationalOrderId)
            where TRequest : IAssignmentOperationalRequest, IRequest<TResponse>
        {
            FillRequestContext(request, userId, companyId, moduleCode, operationalOrderId);

            return await mediator.Send(request);
        }

        protected async Task<IActionResult> AssignAsync<TRequest>(
            TRequest request,
            Guid userId,
            Guid companyId,
            string moduleCode,
            Guid operationalOrderId)
            where TRequest : IAssignmentOperationalRequest, IRequest<Unit>
        {
            FillRequestContext(request, userId, companyId, moduleCode, operationalOrderId);

            await mediator.Send(request);

            return Ok();
        }

        protected async Task<NoContentResult> RemoveAsync<TRequest>(
            TRequest request,
            Guid userId,
            Guid companyId,
            string moduleCode,
            Guid operationalOrderId)
            where TRequest : IAssignmentOperationalRequest, IRequest<bool>
        {
            FillRequestContext(request, userId, companyId, moduleCode, operationalOrderId);

            await mediator.Send(request);

            return NoContent();
        }

        private static void FillRequestContext(
            IAssignmentOperationalRequest request,
            Guid userId,
            Guid companyId,
            string moduleCode,
            Guid operationalOrderId)
        {
            request.CompanyId = companyId;
            request.ModuleCode = moduleCode;
            request.OperationalOrderId = operationalOrderId;
            request.UserId = userId;
        }
    }
}
