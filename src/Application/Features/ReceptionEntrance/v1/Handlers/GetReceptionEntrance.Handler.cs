using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Warehouse.Api.Domain.Entities.Bases;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Queries;

namespace ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Handlers
{
    public class GetReceptionEntrancesHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper) : BaseValidatorHandler<GetReceptionEntrancesQuery, ReceptionEntranceListResponseDto>(_unitOfWork, _errorManager)
    {
        public override async Task<ReceptionEntranceListResponseDto> Handle(GetReceptionEntrancesQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var isSearch = HasSearchFilters(request);

            var query = BuildBaseQuery(isSearch);

            query = ApplyFilters(query, request, isSearch);

            var totalRecords = await query.CountAsync(cancellationToken);

            var receptionEntrances = await query
                .OrderByDescending(purs => purs.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var receptionEntrancesMapped = _mapper.Map<List<ReceptionEntranceDto>>(receptionEntrances);

            var stats = await CalculateStatsAsync(request, isSearch, cancellationToken);

            return new ReceptionEntranceListResponseDto
            {
                Data = receptionEntrancesMapped,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Total = totalRecords,
                Stats = stats
            };
        }

        private IQueryable<ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance> BuildBaseQuery(bool includeAllStates)
        {
            var query = _unitOfWork.ReceptionEntrance.Entities
                .Include(reception => reception.ReceptionTransport)
                .Include(reception => reception.OperationalOrders)
                .AsNoTracking();

            return includeAllStates
                ? query
                : query.Where(reception => reception.IsActive && reception.DeletedAt == null);
        }

        private static bool HasSearchFilters(GetReceptionEntrancesQuery request) =>
            !string.IsNullOrWhiteSpace(request.PlateNumber) ||
            !string.IsNullOrWhiteSpace(request.ContainerNumber) ||
            !string.IsNullOrWhiteSpace(request.ReceptionCode) ||
            !string.IsNullOrWhiteSpace(request.DocumentNumber) ||
            request.DocumentType.HasValue;

        #region Filtros de busqueda
        private static IQueryable<ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance> ApplyFilters(
            IQueryable<ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance> query,
            GetReceptionEntrancesQuery request,
            bool isSearch)
        {
            if (!isSearch)
            {
                var targetDate = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var start = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var end = start.AddDays(1);

                query = query.Where(reception => reception.CreatedAt >= start && reception.CreatedAt < end);
            }
            else if (request.Date.HasValue)
            {
                var targetDate = request.Date.Value;
                var start = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var end = start.AddDays(1);

                query = query.Where(reception => reception.CreatedAt >= start && reception.CreatedAt < end);
            }

            return ApplyNonDateFilters(query, request);
        }

        private static IQueryable<ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance> ApplyNonDateFilters(
            IQueryable<ERP.Core.Database.Domain.Entities.Warehouse.ReceptionEntrance> query,
            GetReceptionEntrancesQuery request)
        {
            if (request.DocumentType.HasValue)
            {
                query = query.Where(reception => reception.OperationalOrders
                    .Any(po => po.DocumentType == request.DocumentType));
            }

            if (!string.IsNullOrWhiteSpace(request.PlateNumber))
            {
                query = query.Where(reception =>
                    reception.ReceptionTransport.VehiclePlateNumber.Contains(request.PlateNumber));
            }

            if (!string.IsNullOrWhiteSpace(request.ContainerNumber))
            {
                query = query.Where(reception =>
                    reception.ContainerNumber.Contains(request.ContainerNumber));
            }

            if (!string.IsNullOrWhiteSpace(request.ReceptionCode))
            {
                query = query.Where(reception =>
                    reception.ReceptionCode != null && reception.ReceptionCode.Contains(request.ReceptionCode));
            }

            if (!string.IsNullOrWhiteSpace(request.DocumentNumber))
            {
                query = query.Where(reception => reception.OperationalOrders.Any(po =>
                    po.DocumentNumber != null && po.DocumentNumber.Contains(request.DocumentNumber)));
            }

            return query;
        }
        #endregion

        #region Stats
        private async Task<ReceptionEntranceDto.Stats> CalculateStatsAsync(
            GetReceptionEntrancesQuery request,
            bool isSearch,
            CancellationToken cancellationToken)
        {
            var stats = new ReceptionEntranceDto.Stats();

            var baseQuery = _unitOfWork.ReceptionEntrance.Entities
                .Include(r => r.ReceptionTransport)
                .AsNoTracking();

            if (!isSearch)
            {
                baseQuery = baseQuery.Where(r => r.IsActive && r.DeletedAt == null);
            }

            var filteredQuery = ApplyNonDateFilters(baseQuery, request);

            var targetDateOnly = request.Date.HasValue
                ? request.Date.Value
                : DateOnly.FromDateTime(DateTime.UtcNow);

            var dayStart = new DateTime(targetDateOnly.Year, targetDateOnly.Month, targetDateOnly.Day, 0, 0, 0, DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);

            stats.TotalEntries = await filteredQuery
                .Where(r => r.CreatedAt >= dayStart && r.CreatedAt < dayEnd)
                .CountAsync(cancellationToken);

            var accumulated = filteredQuery.Where(r => r.CreatedAt < dayEnd);

            stats.TotalsInPlant = await accumulated
                .Where(r => r.ReceptionTransport.TransportUnit == TransportUnit.Van &&
                            (r.ReceptionTransport.VehicleExitDate == null ||
                             r.ReceptionTransport.VehicleExitDate > targetDateOnly))
                .CountAsync(cancellationToken);

            stats.TotalDispatched = await filteredQuery
                .Where(r => r.CreatedAt < dayEnd &&
                            r.ReceptionTransport.TransportUnit == TransportUnit.Van &&
                            r.ReceptionTransport.VehicleExitDate == targetDateOnly)
                .CountAsync(cancellationToken);

            stats.ContainersOnSite = await accumulated
                .Where(r => r.ReceptionTransport.TransportUnit == TransportUnit.Container &&
                            (r.ReceptionTransport.ContainerExitDate == null ||
                             r.ReceptionTransport.ContainerExitDate > targetDateOnly))
                .CountAsync(cancellationToken);

            stats.ContainersDispatched = await filteredQuery
                .Where(r => r.CreatedAt < dayEnd &&
                            r.ReceptionTransport.TransportUnit == TransportUnit.Container &&
                            r.ReceptionTransport.ContainerExitDate == targetDateOnly)
                .CountAsync(cancellationToken);

            return stats;
        }
        #endregion Stats
    }
}