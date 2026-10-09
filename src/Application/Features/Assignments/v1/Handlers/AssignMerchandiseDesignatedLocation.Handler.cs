using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Services;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

using ERP.Core.Warehouse.Api.Application.Commons.Options;
using ERP.Core.Warehouse.Api.Application.Commons.Mappings;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Commands;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class AssignMerchandiseDesignatedLocationHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, ICodeGenerator _codeGenerator, IOptions<QrConfig> _qrConfig)
        : BaseValidatorHandler<AssignMerchandiseDesignatedLocationCommand, AssignMerchandiseDesignatedLocationDto>(_unitOfWork, _errorManager)
    {
        private static readonly JsonSerializerOptions SnakeCaseOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public override async Task<AssignMerchandiseDesignatedLocationDto> Handle(AssignMerchandiseDesignatedLocationCommand request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
            if (!access.IsSuccess) return access.ErrorResponse!;

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Where(a => a.Id == request.AssignmentOperationalId
                    && a.OperationalOrderId == request.OperationalOrderId
                    && a.IsActive
                    && a.DeletedAt == null)
                .FirstOrDefaultAsync(ct);

            if (assignment is null)
                return _errorManager.ThrowNotFound<AssignMerchandiseDesignatedLocationDto>(
                    "No se encontró la asignación solicitada.", "ERP:ASSIGNMENT_NOT_FOUND");

            if (assignment.Status != AssignmentOperationalStatus.InProgress)
                return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                    "La asignación no está en proceso.", "ERP:ASSIGNMENT_NOT_IN_PROGRESS");

            string? qrCode = null;
            string? barCode = null;

            if (request.Sections.Count > 0)
            {
                var allPositionIds = request.GetAllPositionIds();
                if (allPositionIds.Distinct().Count() != allPositionIds.Count)
                    return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                        "No puede asignar la misma posición más de una vez.", "ERP:DUPLICATED_POSITIONS");

                var sectionIds = request.Sections.ConvertAll(s => s.SectionId).Distinct().ToList();

                var sections = await _unitOfWork.Sections.Entities
                    .AsSingleQuery()
                    .Where(s => sectionIds.Contains(s.Id))
                    .Include(s => s.Lots.Where(l => l.DeletedAt == null))
                        .ThenInclude(l => l.Positions!.Where(p => p.DeletedAt == null))
                    .Include(s => s.Racks.Where(r => r.DeletedAt == null))
                        .ThenInclude(r => r.Positions.Where(p => p.DeletedAt == null))
                    .ToListAsync(ct);

                if (sections.Count != sectionIds.Count)
                    return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                        "Una o más secciones no fueron encontradas.", "ERP:SECTION_NOT_FOUND");

                var lotsPositionsToReserve = new List<(LotsPositions Position, Guid SectionId)>();
                var racksPositionsToReserve = new List<(RackPositions Position, Guid SectionId)>();

                foreach (var requestedSection in request.Sections)
                {
                    var section = sections.FirstOrDefault(s => s.Id == requestedSection.SectionId)!;

                    if (assignment.WarehouseId.HasValue && section.WarehouseId != assignment.WarehouseId.Value)
                        return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                            "Una o más secciones no pertenecen al almacén de la asignación.", "ERP:POSITION_SECTION_MISMATCH");

                    if (requestedSection.Tramos.Count > 0 && section.SectionStorageType != SectionStorageType.Lots)
                        return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                            "La sección indicada no es una sección de tramos.", "ERP:POSITION_SECTION_MISMATCH");

                    if (requestedSection.Racks.Count > 0 && section.SectionStorageType != SectionStorageType.Racks)
                        return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                            "La sección indicada no es una sección de racks.", "ERP:POSITION_SECTION_MISMATCH");

                    foreach (var tramoBlock in requestedSection.Tramos)
                    {
                        var lot = section.Lots.FirstOrDefault(l => l.Id == tramoBlock.BlockId);
                        if (lot is null)
                            return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                                "Uno o más tramos no pertenecen a la sección indicada.", "ERP:POSITION_SECTION_MISMATCH");

                        foreach (var positionId in tramoBlock.PositionIds)
                        {
                            var position = lot.Positions!.FirstOrDefault(p => p.Id == positionId);
                            if (position is null)
                                return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                                    $"La posición {positionId} no fue encontrada en el tramo indicado.", "ERP:POSITION_NOT_FOUND");

                            if (position.Status != RackStatus.Available)
                                return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                                    $"La posición {position.PositionCode} no está disponible.", "ERP:POSITION_NOT_AVAILABLE");

                            lotsPositionsToReserve.Add((position, section.Id));
                        }
                    }

                    foreach (var rackBlock in requestedSection.Racks)
                    {
                        var rack = section.Racks.FirstOrDefault(r => r.Id == rackBlock.BlockId);
                        if (rack is null)
                            return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                                "Uno o más racks no pertenecen a la sección indicada.", "ERP:POSITION_SECTION_MISMATCH");

                        foreach (var positionId in rackBlock.PositionIds)
                        {
                            var position = rack.Positions.FirstOrDefault(p => p.Id == positionId);
                            if (position is null)
                                return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                                    $"La posición {positionId} no fue encontrada en el rack indicado.", "ERP:POSITION_NOT_FOUND");

                            if (position.Status != RackStatus.Available)
                                return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                                    $"La posición {position.PositionCode} no está disponible.", "ERP:POSITION_NOT_AVAILABLE");

                            racksPositionsToReserve.Add((position, section.Id));
                        }
                    }
                }

                foreach (var (Position, SectionId) in lotsPositionsToReserve)
                {
                    Position.Status = RackStatus.Reserved;
                    await _unitOfWork.LotsPositions.UpdateAsync(Position);
                    await _unitOfWork.AssignmentStockPlacements.AssignStockPlacement(Position.ToPlacedStockPlacement(assignment.Id, request.UserId, SectionId));
                }

                foreach (var (Position, SectionId) in racksPositionsToReserve)
                {
                    Position.Status = RackStatus.Reserved;
                    await _unitOfWork.RackPositions.UpdateAsync(Position);
                    await _unitOfWork.AssignmentStockPlacements.AssignStockPlacement(Position.ToPlacedStockPlacement(assignment.Id, request.UserId, SectionId));
                }

                var redirectUrl = string.Empty;
                var companyAlias = access.Company.Alias;

                if (
                    _qrConfig.Value.Clients.TryGetValue(request.CompanyId.ToString(), out var client)
                    && !string.IsNullOrWhiteSpace(client.BaseRedirectUrl)
                    && !string.IsNullOrWhiteSpace(companyAlias)
                )
                {
                    var baseUrl = client.BaseRedirectUrl.TrimEnd('/');
                    var alias = Uri.EscapeDataString(companyAlias.Trim('/'));

                    redirectUrl = $"{baseUrl}/{alias}/dashboard/warehouse-mga/ticket";
                }

                var logo = access.Company.ImageUrl;

                var qr = await _codeGenerator.GenerateQrCodeAsync(redirectUrl, logo);
                var bar = await _codeGenerator.GenerateBarcodeAsync(logo);

                await _unitOfWork.Codes.GenerateCode((assignment.Id, CodesType.Qr, qr).ToCodesEntity());
                await _unitOfWork.Codes.GenerateCode((assignment.Id, CodesType.Bar, bar).ToCodesEntity());

                qrCode = qr.Code;
                barCode = bar.Code;
            }

            if (request.MerchandiseType.HasValue || request.Pallets is not null)
            {
                var data = DeserializeAdditionalData(assignment.AdditionalData);

                if (request.MerchandiseType.HasValue)
                {
                    assignment.MerchandiseType = request.MerchandiseType.Value;
                }

                if (request.Pallets is not null)
                {
                    foreach (var pallet in request.Pallets)
                    {
                        if (pallet.Delete)
                        {
                            var existingToRemove = data.PositionatingInformation.FirstOrDefault(p => p.Type == pallet.Type);
                            if (existingToRemove is not null)
                            {
                                data.PositionatingInformation.Remove(existingToRemove);
                            }
                            continue;
                        }

                        var target = data.PositionatingInformation.FirstOrDefault(p => p.Type == pallet.Type);

                        if (target is null)
                        {
                            if (assignment.MerchandiseType == UnloadingMerchandiseType.Bulk && pallet.BulksPerPallet is not > 0)
                            {
                                return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                                    "Debe indicar la cantidad de bultos por polín para mercadería a granel.",
                                    "ERP:INVALID_BULKS_PER_PALLET");
                            }

                            target = new PositionatingInformation { Type = pallet.Type };
                            data.PositionatingInformation.Add(target);
                        }

                        if (pallet.CountPallets.HasValue)
                        {
                            target.CountPallets = pallet.CountPallets.Value;
                        }

                        if (target.Type == PalletType.Standard)
                        {
                            target.Width = 1;
                            target.Length = 1.2m;
                        }
                        else
                        {
                            if (pallet.Width.HasValue)
                            {
                                target.Width = pallet.Width.Value;
                            }

                            if (pallet.Length.HasValue)
                            {
                                target.Length = pallet.Length.Value;
                            }
                        }

                        if (assignment.MerchandiseType == UnloadingMerchandiseType.Bulk && pallet.BulksPerPallet.HasValue)
                        {
                            target.BulksPerPallet = pallet.BulksPerPallet.Value;
                        }
                    }
                }

                if (assignment.MerchandiseType == UnloadingMerchandiseType.Armed)
                {
                    foreach (var pallet in data.PositionatingInformation)
                    {
                        pallet.BulksPerPallet = null;
                    }
                }

                if (!IsValidPositionatingInformation(data.PositionatingInformation, assignment.MerchandiseType))
                {
                    return _errorManager.ThrowBadRequest<AssignMerchandiseDesignatedLocationDto>(
                        "La información de polines no es coherente con el tipo de mercadería.",
                        "ERP:INVALID_POSITIONATING_INFORMATION");
                }

                assignment.HasPositionatingInformation = data.PositionatingInformation.Count > 0;
                assignment.AdditionalData = JsonSerializer.Serialize(data, SnakeCaseOptions);
            }

            await _unitOfWork.AssignmentOperationals.UpdateAsync(assignment);
            await _unitOfWork.SaveChangesAsync(ct);

            return new AssignMerchandiseDesignatedLocationDto
            {
                CodeQr = qrCode,
                CodeBar = barCode
            };
        }

        #region JSON adicional

        private static AdditionalDataAssingmentOperational DeserializeAdditionalData(string? additionalDataJson)
        {
            if (string.IsNullOrWhiteSpace(additionalDataJson))
            {
                return new AdditionalDataAssingmentOperational();
            }

            var data = JsonSerializer.Deserialize<AdditionalDataAssingmentOperational>(additionalDataJson, SnakeCaseOptions)
                ?? new AdditionalDataAssingmentOperational();

            data.PositionatingInformation ??= [];

            return data;
        }

        private static bool IsValidPositionatingInformation(List<PositionatingInformation> pallets, UnloadingMerchandiseType merchandiseType)
        {
            foreach (var pallet in pallets)
            {
                if (pallet.CountPallets <= 0)
                {
                    return false;
                }

                if (pallet.Type == PalletType.Oversized && (pallet.Width <= 0 || pallet.Length <= 0))
                {
                    return false;
                }

                if (merchandiseType == UnloadingMerchandiseType.Bulk && pallet.BulksPerPallet is not > 0)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}