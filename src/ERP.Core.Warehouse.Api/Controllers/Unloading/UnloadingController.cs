using Microsoft.AspNetCore.Mvc;
using ERP.Core.Infrastructure.Attributes;
using ERP.Core.Warehouse.Api.Controllers.ApiBase;

namespace ERP.Core.Warehouse.Api.Controllers.Unloading
{
    [HasToken]
    [ApiVersion("1.0")]
    [Route("api/v1/")]
    public class UnloadingController(/*IMediator mediator*/) : ApiControllerBase
    {
        
    }
}
