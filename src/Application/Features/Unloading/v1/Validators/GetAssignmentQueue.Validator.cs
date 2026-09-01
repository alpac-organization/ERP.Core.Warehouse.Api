using ERP.Core.Warehouse.Api.Application.Commons.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Unloading.v1.Validators;

public class GetAssignmentQueueValidator : BasePagedQueryValidator<GetAssignmentQueueQuery>
{
    public GetAssignmentQueueValidator() : base(maxPageSize: 100)
    {
    }
}