using MediatR;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.CustomBranches.v1.Queries;

public class GetCustomBranchesQuery : BaseRequest, IRequest<List<CustomBranchListItemDto>>
{
}