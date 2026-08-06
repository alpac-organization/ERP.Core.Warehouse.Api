using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Handlers
{
    public class GetPurchaseRequestDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetPurchaseRequestDetailsQuery, PurchaseRequestDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseRequestDetailsDto> Handle(GetPurchaseRequestDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .Include(pur => pur.RegistrationUser)
                .Include(pur => pur.Branch)
                .Where(pur => pur.Id == request.PurchaseRequestId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseRequest is null)
            {
                return _errorManager.ThrowBadRequest<PurchaseRequestDetailsDto>("No se encontro el detalle de esta solicitud", "ERP:NOT_FOUND");
            }

            var detailsMapped = _mapper.Map<PurchaseRequestDetailsDto>(purchaseRequest);

            //Obterner la logica del mapeo de los productos.

            var requestedProducts = await _unitOfWork.PurchaseRequestItems.Entities
                .Where(product => product.PurchaseRequestId == purchaseRequest.Id)
                .Include(product => product.UnitMeasure)
                .Include(product => product.Product)
                    .ThenInclude(product => product.Category)
                .ToListAsync(cancellationToken);

            var requestedProductsMapped = _mapper.Map<List<ProductInformation>>(requestedProducts);

            detailsMapped.RequestedProducts = requestedProductsMapped;

            return detailsMapped;
        }
    }
    
}