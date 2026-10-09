using MediatR;
using System.Text.Json.Serialization;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands
{
    public class UpdateAssignmentPositionsCommand : BaseRequest, IRequest<Unit>, IAssignmentOperationalRequest
    {
        [JsonIgnore]
        public Guid OperationalOrderId { get; set; }

        [JsonIgnore]
        public Guid AssignmentOperationalId { get; set; }

        public UnloadingMerchandiseType MerchandiseType { get; set; }

        public List<UpdateInformationPalletsDto> Pallets { get; set; } = [];
    }

    public class UpdateInformationPalletsDto
    {
        public PalletType Type { get; set; }
        public int CountPallets { get; set; }
        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public int? BulksPerPallet { get; set; }
    }
}