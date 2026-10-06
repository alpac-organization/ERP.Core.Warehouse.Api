using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;

using Command = ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands.CreateReceptionEntranceCommand;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class OperationalOrderProfile : Profile
    {
        public OperationalOrderProfile()
        {
            CreateMap<OperationalOrder, OperationalOrderDto>()
                .ForMember(dest => dest.OperationOrderId, opt => opt.MapFrom(src => src.Id));

            CreateMap<OperationalOrder, OperationalOrderDetailsDto>()
                .IncludeBase<OperationalOrder, OperationalOrderDto>()
                .ForPath(dest => dest.ReceptionEntranceInformation, opt => opt.MapFrom(src => src.Reception))
                .ForMember(dest => dest.CustomerInformation, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.CostCenterInformation, opt => opt.MapFrom(src => src.CostCenter));
        }
    }

    public static class OperationalOrderMapper
    {
        public static OperationalOrder ToOperationalOrderEntity(this Command command, Guid costCenterId)
        {
            bool IsConsolidated = command.GeneralInformation.DucatNumbers.Count > 1;

            return new()
            {
                Id = Guid.NewGuid(),
                IsConsolidated = IsConsolidated,
                CostCenterId = costCenterId,
                Status = OperationalOrderStatus.PendingDocument,
                CompanyId = command.CompanyId,
                DocumentType = command.GeneralInformation.DocumentType,
            };
        }
    }
}