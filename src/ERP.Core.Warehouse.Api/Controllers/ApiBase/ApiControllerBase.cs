using MediatR;
using Microsoft.AspNetCore.Mvc;

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
    }
}