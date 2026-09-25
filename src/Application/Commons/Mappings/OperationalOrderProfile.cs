using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;

using Command = ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands.CreateReceptionEntranceCommand;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class OperationalOrderProfile : Profile
    {
        public OperationalOrderProfile()
        {
            CreateMap<OperationalOrder, OperationalOrderDto>()
                .ForMember(dest => dest.OperationOrderId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CustomerInformation, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter)); 
        }
    }

    public static class OperationalOrderMapper
    {
        public static OperationalOrder ToOperationalOrderEntity(this Command command, Guid costCenterId)
        {
            return new()
            {
                Id = Guid.NewGuid(),
                CostCenterId = costCenterId,
                Status = OperationalOrderStatus.PendingDocument,
                DocumentType = command.GeneralInformation.DocumentType
            };
        }
    }
}