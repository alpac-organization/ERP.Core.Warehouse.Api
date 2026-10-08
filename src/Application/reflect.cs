using System;
using System.Reflection;
using ERP.Core.Database.Domain.Entities.Shopping;

class Program {
    static void Main() {
        foreach (var prop in typeof(PurchaseRequest).GetProperties()) {
            Console.WriteLine(prop.Name + " - " + prop.PropertyType.Name);
        }
    }
}
