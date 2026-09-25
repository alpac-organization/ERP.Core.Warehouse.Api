using MediatR;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries
{
    public class GetReceptionEntrancesQuery : BaseRequest, IRequest<PagedResponse<ReceptionEntranceDto>>
    {
        public string? PlateNumber { get; set; }
        public string? DocumentNumber { get; set; }
        public string? ContainerNumber { get; set; }
        public DocumentType? DocumentType { get; set; }

        ///Adminstración.
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }


    public class GetReceptionEntranceDetailQuery : IRequest<ReceptionEntranceDetailDto>
    {
        public Guid CompanyId { get; set; }
        public string ModuleCode { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public Guid RecordId { get; set; }
    }
}