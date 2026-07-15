using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Application.Commons.Interfaces;

public interface IRecordEntranceServices
{
    Task<GetActiveRecordEntranceResponse> GetActiveRecordEntrance();
}