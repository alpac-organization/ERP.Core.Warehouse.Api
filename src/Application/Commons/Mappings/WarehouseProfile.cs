using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class WarehouseProfile : Profile
{
   public WarehouseProfile()
   {
      CreateMap<Warehouses, WarehouseDto>()
      .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Id))
      .ForMember(dest => dest.WarehouseCode, opt => opt.MapFrom(src => src.Code))
      .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.WarehouseName))
      .ForMember(dest => dest.WarehouseType, opt => opt.MapFrom(src => src.WarehouseType))
      .ForMember(dest => dest.SubWarehouses, opt => opt.MapFrom(src => src.SubWarehouses));

      CreateMap<WarehouseCapacity, WarehouseCapacityDto>();

      #region Sections
      CreateMap<Sections, SectionDto>()
          .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
          .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code))
          .ForMember(dest => dest.SectionName, opt => opt.MapFrom(src => src.Name));
      #endregion

      #region Racks
      CreateMap<Racks, RackDto>()
          .ForMember(dest => dest.RackId, opt => opt.MapFrom(src => src.Id))
          .ForMember(dest => dest.UsageProfile, opt => opt.MapFrom(src => src.UsageProfile.ToString()))
          .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
          .ForMember(dest => dest.TotalPositions, opt => opt.MapFrom(src => src.Positions.Count))
          .ForMember(dest => dest.OccupiedPositions, opt => opt.MapFrom(src => src.Positions.Count(p => p.CurrentStock.Any())));

      CreateMap<RackPositions, RackPositionDto>()
          .ForMember(dest => dest.PositionId, opt => opt.MapFrom(src => src.Id));

      CreateMap<Racks, RackSummaryDto>()
          .ForMember(dest => dest.RackId, opt => opt.MapFrom(src => src.Id));

      #endregion

      #region Lots
      CreateMap<Lots, LotDto>()
          .ForMember(d => d.LotId, o => o.MapFrom(s => s.Id))
          .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
          .ForMember(d => d.TotalPositions, o => o.MapFrom(s => s.Positions.Count))
          .ForMember(d => d.OccupiedPositions, o => o.MapFrom(s => s.Positions.Count(p => p.CurrentStock != null)));

      CreateMap<LotsPositions, LotPositionDto>()
          .ForMember(d => d.PositionId, o => o.MapFrom(s => s.Id));

      CreateMap<Lots, LotSummaryDto>()
          .ForMember(d => d.LotId, o => o.MapFrom(s => s.Id))
          .ForMember(d => d.PositionsCount, o => o.MapFrom(s => s.Positions.Count));

      CreateMap<Lots, LotListItemDto>()
          .ForMember(d => d.LotId, o => o.MapFrom(s => s.Id))
          .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
          .ForMember(d => d.TotalPositions, o => o.MapFrom(s => s.Positions.Count))
          .ForMember(d => d.OccupiedPositions, o => o.MapFrom(s => s.Positions.Count(p => p.CurrentStock != null)));
      #endregion
   }
}

public static class WarehouseMapper
{
   public static Warehouses ToWarehouseEntity(this Commands.RegisterWarehouseCommand command)
   {
      var warehouseId = Guid.NewGuid();

      var details = new WarehouseDetails
      {
         Id = Guid.NewGuid(),
         WarehouseId = warehouseId,
         WitdhMetres = command.WarehouseDetails.WidthMetres,
         LengthMetres = command.WarehouseDetails.LengthMetres,
         RampsCount = command.WarehouseDetails.RampsCount,
         ParkingSpacesCount = command.WarehouseDetails.ParkingSpacesCount
      };

      return new()
      {
         Id = warehouseId,
         Code = command.Code,
         IsActive = true,
         WarehouseName = command.WarehouseName,
         BranchId = command.BranchId,
         WarehouseType = command.WarehouseType,
         ParentWarehouseId = command.ParentWarehouseId,

         Details = details,
         Capacity = details.ToCalculatedCapacity(warehouseId)
      };
   }

   public static WarehouseCapacity ToCalculatedCapacity(this WarehouseDetails details, Guid warehouseId)
   {
      var totalArea = details.WitdhMetres * details.LengthMetres;

      return new WarehouseCapacity
      {
         Id = Guid.NewGuid(),
         WarehouseId = warehouseId,
         TotalAreaM2 = totalArea,
         UsableAreaM2 = null, // aún no hay secciones/racks para descontar
         UnusableAreaM2 = null,
         TotalMaxPolines = null,
         CurrentPolinesStored = null,
         LastCalculatedAt = NicaraguaClock.Now
      };
   }
}

#region Sections
public static class SectionMapper
{
   public static Sections ToSectionEntity(this Commands.RegisterSectionCommand command, string enabledByUserName)
   {
      var sectionId = Guid.NewGuid();

      SectionOverflowCapacity? overflowCapacity = null;

      if (command.SectionType == SectionType.Aisle && command.OverflowCapacity is not null)
      {
         var nowNica = NicaraguaClock.Now;

         overflowCapacity = new SectionOverflowCapacity
         {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            AllowsOverflowStorage = command.OverflowCapacity.AllowsOverflowStorage,
            IsOverflowEnabled = command.OverflowCapacity.IsOverflowEnabled,
            MaxOverflowPolines = command.OverflowCapacity.MaxOverflowPolines,
            EnabledByUserName = enabledByUserName,
            EnabledDate = DateOnly.FromDateTime(nowNica),
            EnabledTime = TimeOnly.FromDateTime(nowNica)
         };
      }

      return new()
      {
         Id = sectionId,
         IsActive = true,
         Code = command.Code,
         Name = command.Name,
         SectionType = command.SectionType,
         StorageType = command.StorageType,
         WidthMetres = command.WidthMetres,
         LengthMetres = command.LengthMetres,
         WarehouseId = command.WarehouseId,

         OverflowCapacity = overflowCapacity
      };
   }
}
#endregion

#region Racks
public static class RackMapper
{
   private const int RackPositionCodePadLength = 2;
   public static Racks ToRackEntity(this RegisterRackCommand command)
   {
      var rackId = Guid.NewGuid();

      return new()
      {
         Id = rackId,
         SectionId = command.SectionId,
         Code = command.Code.Trim(),
         WidthMetres = command.WidthMetres,
         LengthMetres = command.LengthMetres,
         HeightMetres = command.HeightMetres,
         UsageProfile = command.UsageProfile,
         RowNumber = command.RowNumber,
         LevelNumber = command.LevelNumber,
         MaxPulleys = command.MaxPulleys,
         Status = command.Status,
         UnavailableReason = command.UnavailableReason,
         StatusChangedAt = NicaraguaClock.Now,
         Positions = BuildRackPositions(rackId, command.MaxPulleys)
      };
   }

   public static List<Racks> ToRackEntities(
       this RegisterRacksBulkCommand command,
       string shelfCode,
       int nextDepositNumber,
       IReadOnlyDictionary<int, int> lastRowByLevel)
   {
      var racks = new List<Racks>();
      var depositNumber = nextDepositNumber;
      var now = NicaraguaClock.Now;

      foreach (var level in command.Levels.OrderBy(l => l.LevelNumber))
      {
         var startingRow = lastRowByLevel.GetValueOrDefault(level.LevelNumber, 0) + 1;
         var lastRow = startingRow + level.RacksCount - 1;

         for (var row = startingRow; row <= lastRow; row++)
         {
            var rackId = Guid.NewGuid();

            racks.Add(new Racks
            {
               Id = rackId,
               SectionId = command.SectionId,
               Code = $"{shelfCode}-D{depositNumber}",
               WidthMetres = level.WidthMetres,
               LengthMetres = level.LengthMetres,
               HeightMetres = level.HeightMetres,
               UsageProfile = level.UsageProfile,
               RowNumber = row,
               LevelNumber = level.LevelNumber,
               MaxPulleys = level.MaxPulleys,
               Status = level.Status,
               UnavailableReason = level.UnavailableReason,
               StatusChangedAt = now,
               Positions = BuildRackPositions(rackId, level.MaxPulleys)
            });

            depositNumber++;
         }
      }

      return racks;
   }

   private static List<RackPositions> BuildRackPositions(Guid rackId, int maxPulleys)
   {
      return Enumerable.Range(1, maxPulleys)
          .Select(positionNumber => new RackPositions
          {
             Id = Guid.NewGuid(),
             RackId = rackId,
             PositionNumber = positionNumber,
             PositionCode = positionNumber.ToString().PadLeft(RackPositionCodePadLength, '0'),
             IsBlocked = false
          })
          .ToList();
   }
}

public static class RackDtoMapper
{
   public static RegisterRacksBulkCommand ToCommand(
       this RegisterRacksBulkDto dto,
       Guid sectionId,
       Guid userId,
       Guid companyId,
       string moduleCode)
   {
      return new RegisterRacksBulkCommand
      {
         SectionId = sectionId,
         ShelfCode = dto.ShelfCode,
         StartingDepositNumber = dto.StartingDepositNumber,
         Levels = [.. dto.Levels.Select(l => new RackLevelSpec
            {
                LevelNumber = l.LevelNumber,
                RacksCount = l.RacksCount,
                WidthMetres = l.WidthMetres,
                LengthMetres = l.LengthMetres,
                HeightMetres = l.HeightMetres,
                UsageProfile = l.UsageProfile,
                MaxPulleys = l.MaxPulleys,
                Status = l.Status,
                UnavailableReason = l.UnavailableReason
            })],
         UserId = userId,
         CompanyId = companyId,
         ModuleCode = moduleCode
      };
   }

   public static GetRacksBySectionQuery ToQuery(
       this Guid sectionId,
       Guid userId,
       Guid companyId,
       string moduleCode,
       int? levelNumber,
       RackStatus? status,
       RackUsageProfile? usageProfile,
       decimal? widthMetres,
       decimal? lengthMetres,
       decimal? heightMetres) => new()
       {
          SectionId = sectionId,
          LevelNumber = levelNumber,
          Status = status,
          UsageProfile = usageProfile,
          WidthMetres = widthMetres,
          LengthMetres = lengthMetres,
          HeightMetres = heightMetres,
          UserId = userId,
          CompanyId = companyId,
          ModuleCode = moduleCode
       };
}
#endregion

#region Lots
public static class LotMapper
{
   public static List<Lots> ToLotEntities(this RegisterLotsCommand command)
   {
      var now = NicaraguaClock.Now;

      return command.Groups
          .SelectMany(ExpandGroupCodes)
          .Select(item => BuildLot(command.SectionId, item, now))
          .ToList();
   }

   private const int LotCodePadLength = 2; // tramos van de 01 a 40 por bodega

   private static IEnumerable<(string Code, RegisterLotGroupDto Spec)> ExpandGroupCodes(RegisterLotGroupDto group)
   {
      if (group.Codes is { Count: > 0 })
         return group.Codes.Select(c => (c.Trim(), group));

      var start = group.StartNumber ?? 1;
      var count = group.Count ?? 0;
      var prefix = group.CodePrefix ?? string.Empty;

      return Enumerable.Range(start, count)
          .Select(n => ($"{prefix}{n.ToString().PadLeft(LotCodePadLength, '0')}", group));
   }
   private static Lots BuildLot(Guid sectionId, (string Code, RegisterLotGroupDto Spec) item, DateTime now)
   {
      var lotId = Guid.NewGuid();
      var spec = item.Spec;

      return new Lots
      {
         Id = lotId,
         SectionId = sectionId,
         Code = item.Code,
         WidthMetres = spec.WidthMetres,
         LengthMetres = spec.LengthMetres,
         NominalRows = spec.NominalRows,
         NominalColumns = spec.NominalColumns,
         AllowsStacking = spec.AllowsStacking,
         Status = spec.Status,
         UnavailableReason = spec.UnavailableReason,
         StatusChangedAt = now,
         Positions = Enumerable.Range(1, spec.NominalRows)
              .SelectMany(row => Enumerable.Range(1, spec.NominalColumns).Select(col => (row, col)))
              .Select((rc, index) => new LotsPositions
              {
                 Id = Guid.NewGuid(),
                 LotId = lotId,
                 RowNumber = rc.row,
                 ColumnNumber = rc.col,
                 PositionCode = (index + 1).ToString().PadLeft(2, '0'),
                 AllowsStacking = spec.AllowsStacking,
                 IsBlocked = false
              })
              .ToList()
      };
   }
   public static RegisterLotsCommand ToCommand(
       this RegisterLotsDto dto, Guid sectionId, Guid userId, Guid companyId, string moduleCode) => new()
       {
          SectionId = sectionId,
          Groups = dto.Groups,
          UserId = userId,
          CompanyId = companyId,
          ModuleCode = moduleCode
       };
}
#endregion