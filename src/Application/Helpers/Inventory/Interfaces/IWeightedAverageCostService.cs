namespace ERP.Core.Warehouse.Api.Application.Helpers.Inventory.Interfaces
{
    public interface IWeightedAverageCostService
    {
        WeightedAverageCostResult Calculate(
            decimal currentQuantity,
            decimal currentAverageCost,
            decimal incomingQuantity,
            decimal incomingUnitCost);
    }

    public record WeightedAverageCostResult(
        decimal NewQuantity,
        decimal NewAverageCost,
        decimal NewTotalValue);
}
