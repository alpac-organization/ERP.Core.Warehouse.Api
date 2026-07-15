
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;
using MediatR;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers;

public class GetActiveRecordEntranceHandler(IRecordEntranceServices _recordEntranceServices) 
    : IRequestHandler<GetActiveRecordEntranceQuery, GetActiveReceptionEntranceResponse>
{
    public async Task<GetActiveReceptionEntranceResponse> Handle(GetActiveRecordEntranceQuery request, CancellationToken cancellationToken)
    {
        return await _recordEntranceServices.GetActiveRecordEntrance();
    }
}