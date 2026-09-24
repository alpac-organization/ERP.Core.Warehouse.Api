using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class GetReceptionEntrancesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper): BaseValidatorHandler<GetReceptionEntrancesQuery, PagedResponse<ReceptionEntranceDto>>(_unitOfWork, _errorManager)
    {
        public override async Task<PagedResponse<ReceptionEntranceDto>> Handle(GetReceptionEntrancesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            
            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var receptionEntrancesQuery = _unitOfWork.ReceptionEntrance.Entities
                .Include(reception => reception.ReceptionTransport)
                .Include(reception => reception.OperationalOrders)
                .AsNoTracking();

            //aplicar filtros de busqueda aqui..
            if (request.DocumentType.HasValue)
            {
                receptionEntrancesQuery = receptionEntrancesQuery
                    .Where(
                        reception => reception.OperationalOrders
                            .Any(po => po.DocumentType == request.DocumentType)  
                    );
            }
            
            if (!string.IsNullOrEmpty(request.ContainerNumber))
            {
                receptionEntrancesQuery = receptionEntrancesQuery
                    .Where(reception => reception.ContainerNumber == request.ContainerNumber);
            }

            var totalRecords = await receptionEntrancesQuery.CountAsync(cancellationToken);

            var receptionEntrances = await receptionEntrancesQuery
                .OrderByDescending(purs => purs.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var receptionEntrancesMapped = _mapper.Map<List<ReceptionEntranceDto>>(receptionEntrances);

            return new PagedResponse<ReceptionEntranceDto>(
                receptionEntrancesMapped,
                request.PageNumber,
                request.PageSize,
                totalRecords
            );
        }
    }

}
