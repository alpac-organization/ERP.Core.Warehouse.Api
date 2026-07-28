using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.Quotes.v1.Handlers
{
    public class GetQuoteDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetQuoteDetailsQuery, QuotationDto>(_unitOfWork, _errorManager)
    {
        public override async Task<QuotationDto> Handle(GetQuoteDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var quotation = await _unitOfWork.Quotations.Entities
                .Where(quo => quo.Id == request.QuotationId)
                .Include(quo => quo.Branch)
                .FirstOrDefaultAsync(cancellationToken);

            if (quotation is null)
            {
                return _errorManager.ThrowBadRequest<QuotationDto>("No se encontro registro de esta cotización", "ERP:QUOTE_NOT_FOUND");
            }

            var quotationDto = _mapper.Map<QuotationDto>(quotation);

            var listQuotesDetails = await _unitOfWork.QuotesDetails.Entities
                .Where(quo => quo.QuotationId == quotationDto.QuotationId)
                .Include(quo => quo.Supplier)
                .ToListAsync(cancellationToken);

            var quotesDetailsMapped = _mapper.Map<List<QuotationDetailsDto>>(listQuotesDetails);

            // quotationDto.QuotationDetails = quotesDetailsMapped; 

            return quotationDto;
        }
    }
}