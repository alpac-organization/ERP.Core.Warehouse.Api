using ERP.Core.Warehouse.Api.Application.Helpers.Inventory.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Helpers.Inventory.Services
{
    public class WeightedAverageCostService : IWeightedAverageCostService
    {
        private const int DecimalScale = 2;

        public WeightedAverageCostResult Calculate(
            decimal currentQuantity,
            decimal currentAverageCost,
            decimal incomingQuantity,
            decimal incomingUnitCost)
        {
            var safeCurrentQuantity = currentQuantity < 0 ? 0 : currentQuantity;
            var safeCurrentCost = currentAverageCost < 0 ? 0 : currentAverageCost;
            var safeIncomingQuantity = incomingQuantity <= 0 ? 0 : incomingQuantity;
            var safeIncomingCost = incomingUnitCost < 0 ? 0 : incomingUnitCost;

            if (safeIncomingQuantity == 0)
            {
                return new WeightedAverageCostResult(
                    Round(safeCurrentQuantity),
                    Round(safeCurrentCost),
                    Round(safeCurrentQuantity * safeCurrentCost));
            }

            var newQuantity = safeCurrentQuantity + safeIncomingQuantity;

            if (newQuantity <= 0)
            {
                return new WeightedAverageCostResult(0, 0, 0);
            }

            if (safeCurrentQuantity <= 0)
            {
                var onlyIncomingAverage = Round(safeIncomingCost);
                return new WeightedAverageCostResult(
                    Round(newQuantity),
                    onlyIncomingAverage,
                    Round(newQuantity * onlyIncomingAverage));
            }

            var currentTotalValue = safeCurrentQuantity * safeCurrentCost;
            var incomingTotalValue = safeIncomingQuantity * safeIncomingCost;

            var newAverageCost = Round((currentTotalValue + incomingTotalValue) / newQuantity);
            var newQuantityRounded = Round(newQuantity);

            return new WeightedAverageCostResult(
                newQuantityRounded,
                newAverageCost,
                Round(newQuantityRounded * newAverageCost));
        }

        private static decimal Round(decimal value)
            => Math.Round(value, DecimalScale, MidpointRounding.AwayFromZero);
    }
}
