using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class OperationalOrderProfile : Profile
    {
        public OperationalOrderProfile()
        {
            
        }
    }

    public static class OperationalOrderMapper
    {
        public static OperationalOrder ToOperationalOrderEntity(Guid costCenterId)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                CostCenterId = costCenterId,
                Status = OperationalOrderStatus.InProgress,
            };
        }
    }
}