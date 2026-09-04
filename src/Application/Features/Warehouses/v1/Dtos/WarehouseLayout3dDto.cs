using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos
{
    public class RackLayout3dDto
    {
        public Guid RackId { get; set; }
        public string Code { get; set; } = null!;

        public decimal WidthMetres { get; set; }
        public decimal LengthMetres { get; set; }

        public int MaxPulleys { get; set; }

        public LayoutTransform3DDto Transform { get; set; } = new();
    }

    public class LotLayout3DDto
    {
        public Guid LotId { get; set; }
        public string Code { get; set; } = null!;

        public decimal WidthMetres { get; set; }
        public decimal LengthMetres { get; set; }
        public LayoutTransform3DDto Transform { get; set; } = new();
    }

    public class SectionLayout3dDto
    {
        public Guid SectionId { get; set; }
        public string Code { get; set; } = null!;
        public SectionType SectionType { get; set; }
        public SectionStorageType StorageType { get; set; }

        public decimal WidthMetres { get; set; }
        public decimal LengthMetres { get; set; }
        public LayoutTransform3DDto Transform { get; set; } = new();

        //Content of this section

        public List<LotLayout3DDto> Lots { get; set; } = [];
        public List<RackLayout3dDto> Racks { get; set; } = [];
    }

    public class WarehouseLayout3dDto
    {
        public Guid WarehouseId { get; set; }

        public string Code { get; set; } = null!;
        public decimal WidthMetres { get; set; }
        public decimal LengthMetres { get; set; }

        public List<SectionLayout3dDto> Sections { get; set; } = [];
    }
}