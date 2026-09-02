using System;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Core.Warehouse.Api.Controllers.ApiBase
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        private IMediator? _mediator;

        protected IMediator Mediator => 
            _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

        protected bool TryGetUserId(out Guid userId)
        {
            var userIdStr = HttpContext.Items["UserId"] as string;
            return Guid.TryParse(userIdStr, out userId);
        }

        protected Guid CurrentUserId => TryGetUserId(out var userId) ? userId : Guid.Empty;
    }
}