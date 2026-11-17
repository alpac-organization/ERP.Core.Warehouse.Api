using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Bases;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Equipments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class EquipmentProfile : Profile
    {
        public EquipmentProfile()
        {
            CreateMap<Equipment, EquipmentDto>()
                .ForMember(dest => dest.EquipmentId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.EquipmentBrand, opt => opt.MapFrom(src => src.Brand))
                .ForMember(dest => dest.EquipmentModel, opt => opt.MapFrom(src => src.Model))
                .ForMember(dest => dest.EquipmentCode, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.BranchCode, opt => opt.MapFrom(src => src.Branch.BranchCode))
                .ForMember(dest => dest.UpdatedByUserName, opt => opt.MapFrom(src => src.User!.Fullname))
                .ForMember(dest => dest.EquipmentColor, opt => opt.MapFrom(src => src.Color));

            CreateMap<Equipment, EquipmentInformation>();

        }



        public static Equipment ToEquipmentEntity(EquipmentCommand request, Guid branch)
        {
            return new Equipment
            {
                Id              = Guid.NewGuid(),
                BranchId        = branch,
                Brand           = request.Brand,
                Code            = request.Code,
                Year            = request.Year,
                Model           = request.Model,
                SerialNumber    = request.SerialNumber,
                Status          = EquipmentStatus.Available,
                Color           = request.Color,
                Type            = request.Type,
                IsActive        = true,
                Gasoline        = request.Gasoline,
                Diesel          = request.Diesel,
                Electric        = request.Electric,
                Lpg             = request.Lpg
            };
        }

        public static void ApplyPatch(Equipment entity, PatchEquipmentCommand request)
        {
            if (request.BranchId.HasValue) entity.BranchId = request.BranchId.Value;

            if (request.Brand is not null) entity.Brand = request.Brand;
            if (request.Code is not null) entity.Code = request.Code;
            if (request.Year is not null) entity.Year = request.Year;
            if (request.Model is not null) entity.Model = request.Model;
            if (request.SerialNumber is not null) entity.SerialNumber = request.SerialNumber;
            if (request.Color is not null) entity.Color = request.Color;
            if (request.Status.HasValue) entity.Status = request.Status.Value;
            if (request.Type.HasValue) entity.Type = request.Type.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            if (request.Gasoline.HasValue) entity.Gasoline = request.Gasoline.Value;
            if (request.Diesel.HasValue) entity.Diesel = request.Diesel.Value;
            if (request.Electric.HasValue) entity.Electric = request.Electric.Value;
            if (request.Lpg.HasValue) entity.Lpg = request.Lpg.Value;
        }
    }
}