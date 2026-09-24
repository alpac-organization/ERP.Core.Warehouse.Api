using AutoMapper;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class SuppliersProfile : Profile
    {
        public SuppliersProfile()
        {
            CreateMap<Supplier, SupplierInformation>()
                .ForMember(dest => dest.SupplierId,    opt => opt.MapFrom(src => src.Id))
                .ForPath(dest => dest.SupplierDetailsInformation,    opt => opt.MapFrom(src => src.SupplierDetails));

            CreateMap<SupplierDetails, SupplierDetailsInformation>();
        }
    }
}