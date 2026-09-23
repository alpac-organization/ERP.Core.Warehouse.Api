using AutoMapper;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using Commands = ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings
{
    public class ReceptionEntranceProfile : Profile
    {
        public ReceptionEntranceProfile()
        {
            //Mapper Get entrance.
            CreateMap<ReceptionEntrance, ReceptionEntranceDto>()
                .ForMember(dest => dest.ReceptionEntranceId, opt => opt.MapFrom(src => src.Id));
            
        }
    }

    public static class ReceptionEntranceMapper
    {

        #region Información recepción
        
        public static ReceptionEntrance ToReceptionEntranceEntity(this Commands.CreateReceptionEntranceCommand command)
        {
            return new()
            {
                IsActive = true,
                Id = Guid.NewGuid(),
                SealNumber = command.GeneralInformation.SealNumber,
                DocumentType = command.GeneralInformation.DocumentType,
                CountryOfOrigin = command.GeneralInformation.CountryOrigin,
                CustomBranchId = command.GeneralInformation.CustomBranchId,
                ContainerNumber = command.GeneralInformation.ContainerNumber,
            };
        }

        #endregion

        #region Información de transporte

        public static ReceptionTransportEntrance ToTransportEntranceEntity(this Commands.CreateReceptionEntranceCommand command, Guid receptionEntranceId)
        {
            
            return new()
            {
                Id = Guid.NewGuid(),
                ReceptionEntranceId = receptionEntranceId,
                DriverName = command.TransportInformation.DriverName,
                Transportista = command.TransportInformation.Transportista,
                DriverLicense = command.TransportInformation.DriverLicense,
                TransportUnit = command.TransportInformation.TransportUnit,
                VehiclePlateNumber = command.TransportInformation.VehicleChassisNumber,
                VehicleChassisNumber = command.TransportInformation.VehicleChassisNumber,
            };
        }
        
        #endregion
    }
}

