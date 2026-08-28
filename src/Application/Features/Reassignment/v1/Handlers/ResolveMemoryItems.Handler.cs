using AutoMapper;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Reassignment.v1.Handlers;

public class ResolveMemoryItemsHandler(
    IUnitOfWork unitOfWork,
    IErrorManager errorManager,
    IMapper mapper,
    ResolveMemoryItemProcessor processor)
    : BaseValidatorHandler<ResolveMemoryItemsCommand, List<ReassignmentMemoryItemDto>>(unitOfWork, errorManager)
{
    public override async Task<List<ReassignmentMemoryItemDto>> Handle(
        ResolveMemoryItemsCommand request,
        CancellationToken cancellationToken)
    {
        var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);
        if (!access.IsSuccess) return access.ErrorResponse!;

        await processor.ValidateSession(request.SessionId, request.UserId.ToString(), cancellationToken);

        var nowNica = NicaraguaClock.Now;
        var nowDate = DateOnly.FromDateTime(nowNica);
        var nowTime = TimeOnly.FromDateTime(nowNica);

        var resolvedItems = new List<ReassignmentMemoryItemDto>();

        foreach (var memoryItemId in request.MemoryItemIds)
        {
            var memoryItem = await processor.ValidateMemoryItem(memoryItemId, request.SessionId, cancellationToken);

            await processor.ConfirmDestination(memoryItem, request.UserId.ToString(), nowDate, nowTime, cancellationToken);

            resolvedItems.Add(mapper.Map<ReassignmentMemoryItemDto>(memoryItem));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return resolvedItems;
    }
}
