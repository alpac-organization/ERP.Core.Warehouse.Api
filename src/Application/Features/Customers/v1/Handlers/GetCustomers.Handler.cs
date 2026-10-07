using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Handlers
{
    public class GetCustomersHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetCustomersQuery, PagedResponse<CustomerDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);
            
            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var customerQuery = _unitOfWork.Customers.Entities
                .Where(customer => customer.IsActive)
                .Where(customer => customer.CompanyId == request.CompanyId)
                .AsNoTracking();

            var customers = await customerQuery
                .OrderBy(m => m.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var totalRecords = await customerQuery.CountAsync(cancellationToken);

            var customersMapped = _mapper.Map<List<CustomerDto>>(customers);

            return new PagedResponse<CustomerDto>(
                customersMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}