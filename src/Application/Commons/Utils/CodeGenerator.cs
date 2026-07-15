namespace ERP.Core.Warehouse.Api.Application.Commons.Utils
{
    public static class CodeGenerator
    {
        public static string GenerateWarehouseCode(string lastCode)
        {
            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastCode) && int.TryParse(lastCode, out var lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            // Ejemplo: "000001", "000002", etc. Ajusta el padding según tu necesidad
            return nextNumber.ToString().PadLeft(2, '0');
        }
    }
}