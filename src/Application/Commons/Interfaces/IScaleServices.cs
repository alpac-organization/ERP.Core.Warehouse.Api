namespace ERP.Core.Warehouse.Api.Application.Commons.Interfaces
{
    public interface IScaleServices
    {
        Task<decimal> GetWeightFromTheScale();
    }
}