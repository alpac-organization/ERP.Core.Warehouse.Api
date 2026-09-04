using AutoMapper;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Application.Commons.Utils;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using Commands = ERP.Core.Warehouse.Api.Application.Features.Warehouses.v1.Commands;
using ERP.Core.Database.Domain.ValueObjects;

namespace ERP.Core.Warehouse.Api.Application.Commons.Mappings;

public class WarehouseProfile : Profile
{
   public WarehouseProfile()
   {
      CreateMap<Warehouses, WarehouseDto>()
         .ConvertUsing((source, _, context) => new WarehouseDto
         {
            WarehouseId = source.Id,
            WarehouseName = source.WarehouseName,
            WarehouseCode = source.Code,
            IsActive = source.IsActive,
            WarehouseType = source.WarehouseType,
            IsOwner = source.IsOwner,
            BranchCode = source.Branch?.BranchCode,
            SectionsCount = source.Sections?.Count ?? 0,
            Capacity = source.Capacity is null
               ? null
               : context.Mapper.Map<WarehouseCapacityDto>(source.Capacity),
            HasChildren = source.HasChildren
         });

      CreateMap<WarehouseCapacity, WarehouseCapacityDto>();
      CreateMap<TransformWarehouse3D, LayoutTransform3DDto>();

      #region Sections
      CreateMap<Sections, SectionDto>()
          .ForMember(dest => dest.SectionId, opt => opt.MapFrom(src => src.Id))
          .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.Code))
          .ForMember(dest => dest.SectionName, opt => opt.MapFrom(src => src.Name))
          .ForMember(dest => dest.WidthMetres, opt => opt.MapFrom(src => src.WidthMetres))
          .ForMember(dest => dest.LengthMetres, opt => opt.MapFrom(src => src.LengthMetres))
          .ForMember(dest => dest.Transform, opt => opt.MapFrom(src => src.TransformWarehouse3D));
      #endregion

      #region Racks
      CreateMap<Racks, RackListDto>()
         .ForMember(dest => dest.RackId, opt => opt.MapFrom(src => src.Id))
         .ForMember(dest => dest.Transform, opt => opt.MapFrom(src => src.TransformWarehouse3D))
         .ForMember(dest => dest.Positions, opt => opt.MapFrom(src => src.Positions.OrderBy(p => p.PositionNumber)))
         .AfterMap((src, dest) =>
         {
            dest.TotalPositions = PositionMetrics.Total(src.Positions);
            dest.OccupiedPositions = PositionMetrics.Occupied(
               src.Positions,
               p => p.IsOccupied || p.IsBlocked || p.IsReserved);
         });

      CreateMap<RackPositions, RackPositionDto>()
         .ForMember(dest => dest.PositionId, opt => opt.MapFrom(src => src.Id))
         .ForMember(dest => dest.PositionNumber, opt => opt.MapFrom(src => src.PositionNumber))
         .ForMember(dest => dest.PositionCode, opt => opt.MapFrom(src => src.PositionCode))
         .ForMember(dest => dest.IsBlocked, opt => opt.MapFrom(src => src.IsBlocked))
         .ForMember(dest => dest.BlockReason, opt => opt.MapFrom(src => src.BlockReason))
         .ForMember(dest => dest.IsOccupied, opt => opt.MapFrom(src => src.IsOccupied));
      #endregion

      #region Lots
      CreateMap<Lots, LotDto>()
         .ForMember(d => d.LotId, o => o.MapFrom(s => s.Id))
         .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
         .ForMember(d => d.Transform, o => o.MapFrom(s => s.TransformWarehouse3D))
         .AfterMap((src, dest) =>
         {
            dest.TotalPositions = PositionMetrics.Total(src.Positions);
            dest.OccupiedPositions = PositionMetrics.Occupied(src.Positions, p => p.IsOccupied);
            dest.BlockedPositions = PositionMetrics.Blocked(src.Positions, p => p.IsBlocked);
            dest.FreePositions = PositionMetrics.Free(
               dest.TotalPositions,
               dest.OccupiedPositions,
               dest.BlockedPositions);
         });

      CreateMap<LotsPositions, LotPositionDto>()
          .ForMember(d => d.PositionId, o => o.MapFrom(s => s.Id))
         .ForMember(d => d.RowNumber, o => o.MapFrom(s => s.RowNumber))
         .ForMember(d => d.ColumnNumber, o => o.MapFrom(s => s.ColumnNumber))
         .ForMember(d => d.PositionCode, o => o.MapFrom(s => s.PositionCode))
         .ForMember(d => d.AllowsStacking, o => o.MapFrom(s => s.AllowsStacking))
         .ForMember(d => d.IsBlocked, o => o.MapFrom(s => s.IsBlocked))
         .ForMember(d => d.IsOccupied, o => o.MapFrom(s => s.IsOccupied))
         .ForMember(d => d.BlockReason, o => o.MapFrom(s => s.BlockReason));

      CreateMap<Lots, LotListItemDto>()
         .ForMember(d => d.LotId, o => o.MapFrom(s => s.Id))
         .ForMember(d => d.Code, o => o.MapFrom(s => s.Code))
         .ForMember(d => d.Transform, o => o.MapFrom(s => s.TransformWarehouse3D))
         .ForMember(d => d.WidthMetres, o => o.MapFrom(s => s.WidthMetres))
         .ForMember(d => d.LengthMetres, o => o.MapFrom(s => s.LengthMetres))
         .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
         .AfterMap((src, dest) =>
         {
            dest.TotalPositions = PositionMetrics.Total(src.Positions);
            dest.UsedPositions = PositionMetrics.Occupied(src.Positions, p => p.IsOccupied || p.IsBlocked || p.IsReserved);
            dest.TotalAreaM2 = PositionMetrics.Area(src.WidthMetres, src.LengthMetres);
            dest.UsedAreaM2 = PositionMetrics.UsedArea(dest.TotalAreaM2, dest.TotalPositions, dest.UsedPositions);
            dest.OccupancyPercentage = PositionMetrics.OccupancyPercentage(dest.TotalPositions, dest.UsedPositions);
         });
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
         IsOwner = command.IsOwner,
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
         UsableAreaM2 = 0m, // aún no hay secciones/racks para descontar
         UnusableAreaM2 = totalArea,
         TotalMaxPolines = 0,
         CurrentPolinesStored = 0,
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
         OverflowCapacity = overflowCapacity,
         TransformWarehouse3D = command.LayoutTransform3DDto is null
         ? new() : new TransformWarehouse3D
         {
            PositionX = command.LayoutTransform3DDto.PositionX,
            PositionY = command.LayoutTransform3DDto.PositionY,
            PositionZ = command.LayoutTransform3DDto.PositionZ,
            RotationY = command.LayoutTransform3DDto.RotationY
         }
      };
   }
}
#endregion

#region Racks
public static class RackMapper
{
   public const decimal StandardLevelHeight = 1.70m;

   public static List<Racks> ToRackEntities(this RegisterRacksBulkCommand command)
   {
      var now = NicaraguaClock.Now;
      var racks = new List<Racks>();

      foreach (var placement in command.PlacementsRacks)
      {
         var baseCode = placement.Code.Trim();
         var baseX = placement.LayoutTransform3DDto?.PositionX ?? 0m;
         var baseY = placement.LayoutTransform3DDto?.PositionY ?? 0m;
         var baseZ = placement.LayoutTransform3DDto?.PositionZ ?? 0m;
         var baseRot = placement.LayoutTransform3DDto?.RotationY ?? 0m;

         var orderedLevels = placement.Levels.OrderBy(l => l.LevelNumber).ToList();
         decimal cumulativeHeight = 0m;

         foreach (var level in orderedLevels)
         {
            var rackId = Guid.NewGuid();
            var positionY = baseY + cumulativeHeight;

            racks.Add(new Racks
            {
               Id = rackId,
               SectionId = command.SectionId,
               Code = $"{baseCode}-L{level.LevelNumber}",
               WidthMetres = level.WidthMetres,
               LengthMetres = level.LengthMetres,
               HeightMetres = StandardLevelHeight,
               UsageProfile = level.UsageProfile,
               RowNumber = 1,
               LevelNumber = level.LevelNumber,
               MaxPulleys = level.MaxPulleys,
               Status = level.Status,
               UnavailableReason = level.UnavailableReason,
               StatusChangedAt = now,
               TransformWarehouse3D = new TransformWarehouse3D
               {
                  PositionX = baseX,
                  PositionY = positionY,
                  PositionZ = baseZ,
                  RotationY = baseRot
               },
               Positions = Enumerable.Range(1, level.MaxPulleys).Select(i => new RackPositions
               {
                  Id = Guid.NewGuid(),
                  RackId = rackId,
                  PositionNumber = i,
                  PositionCode = i.ToString().PadLeft(2, '0'),
                  IsBlocked = false,
                  IsOccupied = false
               }).ToList()
            });

            cumulativeHeight += StandardLevelHeight;
         }
      }

      return racks;
   }

   public static RegisterRacksBulkCommand WithContext(
       this RegisterRacksBulkCommand command,
       Guid sectionId, Guid userId, Guid companyId, string moduleCode)
   {
      command.SectionId = sectionId;
      command.UserId = userId;
      command.CompanyId = companyId;
      command.ModuleCode = moduleCode;
      return command;
   }

   public static GetRacksBySectionQuery ToQuery(
       this Guid sectionId, Guid userId, Guid companyId, string moduleCode,
       int? levelNumber, RackStatus? status, RackUsageProfile? usageProfile,
       decimal? widthMetres, decimal? lengthMetres,
       int pageNumber, int pageSize) => new()
       {
          SectionId = sectionId,
          LevelNumber = levelNumber,
          Status = status,
          UsageProfile = usageProfile,
          WidthMetres = widthMetres,
          LengthMetres = lengthMetres,
          PageNumber = pageNumber,
          PageSize = pageSize,
          UserId = userId,
          CompanyId = companyId,
          ModuleCode = moduleCode
       };
}
#endregion
#region Lots
public static class LotMapper
{
   public static Lots ToLotEntity(this RegisterLotCommand command)
   {
      var now = NicaraguaClock.Now;
      var lotId = Guid.NewGuid();

      return new Lots
      {
         Id = lotId,
         SectionId = command.SectionId,
         Code = command.Code.Trim(),
         WidthMetres = command.WidthMetres,
         LengthMetres = command.LengthMetres,
         NominalRows = command.NominalRows,
         NominalColumns = command.NominalColumns,
         AllowsStacking = command.AllowsStacking,
         Status = command.Status,
         UnavailableReason = command.UnavailableReason,
         StatusChangedAt = now,
         TransformWarehouse3D = command.LayoutTransform3DDto is null ? new() : new TransformWarehouse3D
         {
            PositionX = command.LayoutTransform3DDto.PositionX,
            PositionY = command.LayoutTransform3DDto.PositionY,
            PositionZ = command.LayoutTransform3DDto.PositionZ,
            RotationY = command.LayoutTransform3DDto.RotationY
         },
         Positions = Enumerable.Range(1, command.NominalRows)
            .SelectMany(row => Enumerable.Range(1, command.NominalColumns).Select(col => (row, col)))
            .Select((rc, index) => new LotsPositions
            {
               Id = Guid.NewGuid(),
               LotId = lotId,
               RowNumber = rc.row,
               ColumnNumber = rc.col,
               PositionCode = (index + 1).ToString().PadLeft(2, '0'),
               AllowsStacking = command.AllowsStacking,
               IsBlocked = false
            }).ToList()
      };
   }

   public static RegisterLotCommand WithContext(
       this RegisterLotCommand command,
       Guid sectionId,
       Guid userId,
       Guid companyId,
       string moduleCode)
   {
      command.SectionId = sectionId;
      command.UserId = userId;
      command.CompanyId = companyId;
      command.ModuleCode = moduleCode;
      return command;
   }
}
#endregion