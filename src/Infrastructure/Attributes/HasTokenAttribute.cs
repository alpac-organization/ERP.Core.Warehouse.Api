namespace ERP.Core.Manager.Api.Infrastructure.Attributes
{
    // Este atributo solo sirve para marcar qué endpoints queremos proteger
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class HasTokenAttribute : Attribute { }
}