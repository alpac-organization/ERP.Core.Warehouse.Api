using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotations.v1.Handlers
{
    public class GetQuotationsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetQuotationsQuery, PagedResponse<QuotationDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<QuotationDto>> Handle(GetQuotationsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var quotationsQuery = _unitOfWork.Quotations.Entities
                .Where(quo => quo.IsActive)
                .Where(quo => quo.PurchaseRequestItemId == request.PurchaseRequestItemId)
                .Include(quo => quo.Supplier)
                .AsNoTracking();

            var totalRecords = await quotationsQuery.CountAsync(cancellationToken);

            var quotations = await quotationsQuery
                .OrderByDescending(quo => quo.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var quotationsMapped = _mapper.Map<List<QuotationDto>>(quotations);

            return new PagedResponse<QuotationDto>(
                quotationsMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }
}
