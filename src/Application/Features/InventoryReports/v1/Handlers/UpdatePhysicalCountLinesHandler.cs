using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.InventoryReports.v1.Handlers
{
    public class UpdatePhysicalCountLinesHandler(IUnitOfWork unitOfWork, IErrorManager errorManager, IMapper mapper)
        : BaseValidatorHandler<UpdatePhysicalCountLinesCommand, Unit>(unitOfWork, errorManager)
    {
        public override async Task<Unit> Handle(UpdatePhysicalCountLinesCommand request, CancellationToken ct)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode!, ct);
            if (!access.IsSuccess)
                return access.ErrorResponse!;

            var physicalCount = await _unitOfWork.PhysicalCounts.GetByIdAsync(request.PhysicalCountId, ct);
            if (physicalCount == null || physicalCount.CompanyId != request.CompanyId)
            {
                return _errorManager.ThrowBadRequest<Unit>("Levantamiento fisico no encontrado.", "ERP:007");
            }

            if (physicalCount.BranchId != access.Profile.BranchId)
            {
                return _errorManager.ThrowUnauthorized<Unit>(
                    "No tiene acceso para editar levantamientos fisicos de otra sucursal.",
                    "ERP:INVALID_BRANCH_ACCESS");
            }

            if (physicalCount.Status != PhysicalCountStatus.Draft && physicalCount.Status != PhysicalCountStatus.InProgress)
            {
                return _errorManager.ThrowBadRequest<Unit>("No se pueden editar lineas de un levantamiento cerrado.", "ERP:008");
            }

            var items = request.Items.DistinctBy(i => i.PhysicalCountLineId).ToList();
            if (items.Count == 0)
                return Unit.Value;

            var ids = items.Select(i => i.PhysicalCountLineId).ToList();

            var lines = await _unitOfWork.PhysicalCountLines.Entities
                .Where(l => l.PhysicalCountId == request.PhysicalCountId && ids.Contains(l.Id))
                .ToListAsync(ct);

            var linesById = lines.ToDictionary(l => l.Id);

            foreach (var item in items)
            {
                if (!linesById.TryGetValue(item.PhysicalCountLineId, out var line))
                {
                    return _errorManager.ThrowBadRequest<Unit>(
                        "Linea de levantamiento fisico no encontrada.",
                        "ERP:PHYSICAL_COUNT_LINE_NOT_FOUND");
                }

                if (line.PhysicalStock.HasValue)
                {
                    return _errorManager.ThrowBadRequest<Unit>(
                        "La linea ya tiene conteo fisico y no se puede editar.",
                        "ERP:PHYSICAL_COUNT_LINE_ALREADY_COUNTED");
                }
            }

            var countedAt = DateTime.UtcNow;
            var newlyCounted = 0;

            foreach (var item in items)
            {
                var line = linesById[item.PhysicalCountLineId];

                var source = new UpdatePhysicalCountLineSource
                {
                    PhysicalStock = item.PhysicalStock,
                    Observations = item.Observations,
                    UserId = request.UserId,
                    CountedAt = countedAt
                };

                mapper.Map(source, line);

                await _unitOfWork.PhysicalCountLines.UpdateAsync(line);

                if (item.PhysicalStock.HasValue)
                    newlyCounted++;
            }

            if (newlyCounted > 0)
            {
                physicalCount.CompletedLines += newlyCounted;

                if (physicalCount.CompletedLines >= physicalCount.TotalLines)
                {
                    physicalCount.Status = PhysicalCountStatus.Completed;
                    physicalCount.CompletedAt = countedAt;
                    physicalCount.CompletedBy = request.UserId;
                }
                else if (physicalCount.Status == PhysicalCountStatus.Draft)
                {
                    physicalCount.Status = PhysicalCountStatus.InProgress;
                }

                await _unitOfWork.PhysicalCounts.UpdateAsync(physicalCount);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return Unit.Value;
        }
    }
}
