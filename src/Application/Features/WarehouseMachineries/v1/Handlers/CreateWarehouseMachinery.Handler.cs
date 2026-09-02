using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseMachineries.v1.Handlers
{
    public class CreateWarehouseMachineryHandler : BaseValidatorHandler<CreateWarehouseMachineryCommand, bool>
    {
        private readonly IMapper _mapper;

        public CreateWarehouseMachineryHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
            : base(unitOfWork, errorManager)
        {
            _mapper = mapper;
        }

        public override async Task<bool> Handle(CreateWarehouseMachineryCommand request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var machinery = _mapper.Map<WarehouseMachinery>(request);

            await _unitOfWork.WarehouseMachineries.RegisterMachinery(machinery);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
