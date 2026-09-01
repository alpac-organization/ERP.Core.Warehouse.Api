using AutoMapper;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.PurchaseOrders.v1.Handlers
{
    public class GetPurchaseOrderDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetPurchaseOrderDetailsQuery, PurchaseOrderDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PurchaseOrderDetailsDto> Handle(GetPurchaseOrderDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var purchaseOrder = await _unitOfWork.PurchaseOrders.Entities
                .Include(purs => purs.SentByUser)
                    .ThenInclude(user => user.WorkArea)

                .Include(purs => purs.ReviewedByUser)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.Branch)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.WorkArea)
                        .ThenInclude(area => area.CostCenters)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.RegistrationUser)
                        .ThenInclude(user => user.WorkArea)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.UserRevision)
                        .ThenInclude(user => user.WorkArea)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.Product)

                .Include(purs => purs.PurchaseRequest)
                    .ThenInclude(pr => pr.PurchaseRequestItems)
                        .ThenInclude(item => item.UnitMeasure)

                .AsNoTracking()
                .Where(purs => purs.Id == request.PurchaseOrderId)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder is null)
            {
                return _errorManager.ThrowBadRequest<PurchaseOrderDetailsDto>("No se encontro la orden de compra", "ERP:NOT_FOUND");
            }

            return _mapper.Map<PurchaseOrderDetailsDto>(purchaseOrder);
        }
    }
}
