namespace ERP.Core.Warehouse.Api.Application.Features.ServiceOrder.v1.Dtos;

public record CreateServiceOrderResponse(
    Guid ServiceOrderId,
    string Code,
    Guid CustomerId,
    string? Observations
);

public record CreateServiceOrderDto(
    Guid CustomerId,
    string? Observations,
    bool IsCreatedFromPortal = false
);