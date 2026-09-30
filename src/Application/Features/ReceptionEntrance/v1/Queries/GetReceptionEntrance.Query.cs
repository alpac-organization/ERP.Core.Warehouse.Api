using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries
{
    public class GetReceptionEntrancesQuery : BaseRequest, IRequest<PagedResponse<ReceptionEntranceDto>>
    {
        public bool OnlyDay { get; set; } = true;
        public string? PlateNumber { get; set; }
        public string? DocumentNumber { get; set; }
        public string? ContainerNumber { get; set; }
        public DocumentType? DocumentType { get; set; }

        ///Adminstración.
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }
}