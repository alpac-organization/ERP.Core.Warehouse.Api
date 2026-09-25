using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Operations;

using Command = ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Commands.CreateReceptionEntranceCommand;

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