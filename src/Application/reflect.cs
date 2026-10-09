using System;
using System.Reflection;
using ERP.Core.Database.Domain.Entities.Shopping;

namespace ERP.Core.Warehouse.Api.ReflectionTools
{
    class Program
    {
        static void Main()
        {
            foreach (var prop in typeof(PurchaseRequest).GetProperties())
            {
                Console.WriteLine(prop.Name + " - " + prop.PropertyType.Name);
            }
        }
    }
}
