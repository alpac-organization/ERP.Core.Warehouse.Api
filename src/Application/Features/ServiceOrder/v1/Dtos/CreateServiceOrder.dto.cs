namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

public record CreateServiceOrderResponse(
    Guid ServiceOrderId,
    string Code,
    string Status
);