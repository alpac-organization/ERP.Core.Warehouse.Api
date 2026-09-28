using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Handlers
{
    public class GetOperationalOrderDetailsHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) :  BaseValidatorHandler<GetOperationalOrderDetailsQuery, OperationalOrderDetailsDto>(_unitOfWork, _errorManager)
    {
        public override async Task<OperationalOrderDetailsDto> Handle(GetOperationalOrderDetailsQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            if (access.Role?.RoleType == RoleType.Supervisor)
            {
                return _errorManager.ThrowBadRequest<OperationalOrderDetailsDto>("No tienes acceso para verificar esta información", "ERP:INVALID_ACCESS");
            }

            var operationalOrdersQuery = _unitOfWork.OperationalOrders.Entities
                .Include(po => po.Customer)
                .Include(po => po.Warehouse)
                .Include(po => po.Reception)
                    .ThenInclude(reception => reception.ReceptionTransport)
                .Include(po => po.CostCenter)
                .Where(po => po.Id == request.OperationalOrderId);

            var operationalOrder = await operationalOrdersQuery
                .FirstOrDefaultAsync(cancellationToken);

            var operationalOrderMapped = _mapper.Map<OperationalOrderDetailsDto>(operationalOrder);

            return operationalOrderMapped;
        }
    }   
}