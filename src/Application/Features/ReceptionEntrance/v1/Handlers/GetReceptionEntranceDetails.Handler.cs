using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class GetReceptionEntranceDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper): BaseValidatorHandler<GetReceptionEntranceDetailsQuery, ReceptionEntranceDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<ReceptionEntranceDetailsDto> Handle(GetReceptionEntranceDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            
            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var receptionEntrance = await _unitOfWork.ReceptionEntrance.Entities
                .Include(reception => reception.User)
                .Include(reception => reception.CustomsBranches)
                .Include(reception => reception.ReceptionTransport)
                .Where(reception => reception.IsActive)
                .Where(reception => reception.Id == request.ReceptionEntranceId)
                .FirstOrDefaultAsync(cancellationToken);

            var receptionMapped = _mapper.Map<ReceptionEntranceDetailsDto>(receptionEntrance);

            return receptionMapped;
        }
    }

}
