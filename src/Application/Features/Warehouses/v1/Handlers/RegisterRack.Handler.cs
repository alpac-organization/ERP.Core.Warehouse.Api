using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Handlers;

public class RegisterRacksBulkHandler(IUnitOfWork unitOfWork, IErrorManager errorManager) : BaseValidatorHandler<RegisterRacksBulkCommand, bool>(unitOfWork, errorManager)
{
    public override async Task<bool> Handle(RegisterRacksBulkCommand request, CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

        if (!access.IsSuccess) return access.ErrorResponse!;

        var sectionInfo = await _unitOfWork.Sections.Entities
            .Where(s => s.Id == request.SectionId && s.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        // your code here


        return true;
    }
}
