using System;
using System.Collections.Generic;
using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Queries
{
    public class GetWarehouseMachineriesQuery : BaseRequest, IRequest<IEnumerable<WarehouseMachineryListDto>>
    {
    }
}
