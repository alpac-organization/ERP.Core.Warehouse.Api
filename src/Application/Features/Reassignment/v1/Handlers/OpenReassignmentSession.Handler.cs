using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class OpenReassignmentSessionHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
    : BaseValidatorHandler<OpenReassignmentSessionCommand, ReassignmentSessionDto>(unitOfWork, errorManager)
{
    public override async Task<ReassignmentSessionDto> Handle(OpenReassignmentSessionCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        var warehouse = await _unitOfWork.Warehouses.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.WarehouseId && w.IsActive, cancellationToken);

        if (warehouse is null)
            return _errorManager.ThrowBadRequest<ReassignmentSessionDto>(
                "El almacén indicado no existe o no está activo.",
                "ERP:WAREHOUSE_NOT_FOUND");

        var session = request.ToSessionEntity(request.UserId.ToString());

        await _unitOfWork.ReassignmentSessions.InsertReassignmentSession(session);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<ReassignmentSessionDto>(session);
    }
}
