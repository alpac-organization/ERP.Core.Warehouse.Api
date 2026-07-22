using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Handlers
{
    public class GetQuotesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetQuotesQuery, PagedResponse<QuotationDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<QuotationDto>> Handle(GetQuotesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var quotationsQuery = _unitOfWork.Quotations.Entities
                .Include(quo => quo.Branch)
                .AsNoTracking();

            if (request.BranchId.HasValue)
            {
                quotationsQuery = quotationsQuery 
                    .Where(quo => quo.BranchId == request.BranchId);
            }

            if (!string.IsNullOrEmpty(request.QuoteCode))
            {
                quotationsQuery = quotationsQuery 
                    .Where(quo => quo.QuotationCode == request.QuoteCode);
            }

            var totalRecords = await quotationsQuery.CountAsync(cancellationToken);

            var quotations = await quotationsQuery
                .OrderByDescending(quo => quo.QuoteDate)
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