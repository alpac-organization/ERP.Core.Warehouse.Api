using AutoMapper;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.Customers.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customers, CustomerDto>()
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Id));
        }
    }

    public static class CustomerMapper
    {
        public static Customers ToCustomerEntity(this RegisterCustomerCommand command, Guid companyId, string code)
        {
            return new Customers
            {
                Id                   = Guid.NewGuid(),
                IsActive             = true,
                LegalName            = command.LegalName?.Trim(),
                CIFCode              = code,
                IdentificationNumber = command.IdentificationNumber?.Trim(),
                CustomerType         = command.CustomerType,
                IdentificationType   = command.IdentificationType,
                CompanyId            = companyId
            };
        }
    }
}