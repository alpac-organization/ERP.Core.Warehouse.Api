using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Inventory;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Helpers.Inventory.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public static class KardexMapper
    {
        public static Kardex ToEntryKardexEntity(
            Product product,
            decimal incomingQuantity,
            decimal incomingUnitCost,
            WeightedAverageCostResult weightedCost,
            Guid movementByUserId,
            Guid workAreaId,
            Guid costCenterId,
            DateOnly transactionDate,
            TimeOnly transactionTime,
            Guid? inventoryEntryId = null)
        {
            var roundedUnits = RoundCurrency(incomingQuantity);
            var roundedUnitCost = RoundCurrency(incomingUnitCost);
            var totalInput = RoundCurrency(roundedUnits * roundedUnitCost);

            return new Kardex
            {
                Id = Guid.NewGuid(),
                KardexType = product.ProductUsageType == ProductUsageType.Operational
                    ? KardexType.Operational
                    : KardexType.Administrative,
                ReferenceType = ReferenceType.FT,
                MovementType = KardexMovementType.Buys,
                Units = roundedUnits,
                UnitCost = roundedUnitCost,
                TotalInput = totalInput,
                TotalOutput = 0m,
                TotalUnits = weightedCost.NewQuantity,
                AverageCost = weightedCost.NewAverageCost,
                TotalKardex = weightedCost.NewTotalValue,
                TransactionDate = transactionDate,
                TransactionTime = transactionTime,
                ProductId = product.Id,
                MovementByUserId = movementByUserId,
                WorkAreaId = workAreaId,
                CostCenterId = costCenterId,
                InventoryEntryId = inventoryEntryId
            };
        }

        public static Kardex ToOutputKardexEntity(
            ProductStock stock,
            decimal dispatchedQuantity,
            InventoryOutputs output,
            Guid movementByUserId,
            DateTime transactionDateTime,
            decimal previousUnits,
            decimal previousCost)
        {
            var roundedUnits = RoundCurrency(dispatchedQuantity);
            var unitCost = RoundCurrency(previousCost);
            var previousKardex = previousUnits * previousCost;
            var totalOutput = RoundCurrency(roundedUnits * unitCost);

            var newUnits = RoundCurrency(stock.AvailableQuantity);
            var newKardex = previousKardex - totalOutput;
            var newAvgCost = newUnits > 0m ? RoundCurrency(newKardex / newUnits) : unitCost;

            return new Kardex
            {
                Id = Guid.NewGuid(),
                KardexType = stock.Product?.ProductUsageType == ProductUsageType.Administrative
                    ? KardexType.Administrative
                    : KardexType.Operational,
                ReferenceType = output.FuelRequestId.HasValue
                    ? ReferenceType.BL
                    : output.SupplyRequestId.HasValue
                        ? ReferenceType.RQ
                        : ReferenceType.None,
                MovementType = KardexMovementType.Exit,
                Units = roundedUnits,
                UnitCost = unitCost,
                TotalInput = 0m,
                TotalOutput = totalOutput,
                TotalUnits = newUnits,
                AverageCost = newAvgCost,
                TotalKardex = RoundCurrency(newUnits * newAvgCost),
                TransactionDate = DateOnly.FromDateTime(transactionDateTime),
                TransactionTime = TimeOnly.FromDateTime(transactionDateTime),
                ProductId = stock.ProductId,
                MovementByUserId = movementByUserId,
                WorkAreaId = output.WorkAreaId,
                CostCenterId = output.CostCenterId,
                InventoryOutputId = output.Id
            };
        }

        private static decimal RoundCurrency(decimal value)
            => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
