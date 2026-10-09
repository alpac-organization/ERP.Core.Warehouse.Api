using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ERP.Core.Application.Commons.Interfaces;

using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Operations;
using ERP.Core.Database.Domain.Enums;

using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Dtos;
using ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Queries;
using ERP.Core.Warehouse.Api.Application.Features.OperationalOrders.v1.Dtos;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers
{
    public class GetPositionDetailHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager, IMapper _mapper)
        : BaseValidatorHandler<GetPositionDetailQuery, PositionDetailDto>(_unitOfWork, _errorManager)
    {
        public override async Task<PositionDetailDto> Handle(GetPositionDetailQuery request, CancellationToken cancellationToken)
        {
            var access = await ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, cancellationToken);

            if (!access.IsSuccess)
            {
                return access.ErrorResponse!;
            }

            var placement = await _unitOfWork.AssignmentStockPlacements.Entities
                .Where(p => p.LotPositionId == request.PositionId || p.RackPositionId == request.PositionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (placement is null)
            {
                return _errorManager.ThrowBadRequest<PositionDetailDto>(
                    "La posición no tiene asignaciones asociadas.", "ERP:POSITION_WITHOUT_ASSIGNMENT");
            }

            var assignment = await _unitOfWork.AssignmentOperationals.Entities
                .Include(a => a.OperationalOrder)
                    .ThenInclude(po => po.Customer)
                .Include(a => a.OperationalOrder)
                    .ThenInclude(po => po.Reception)
                    .ThenInclude(reception => reception.ReceptionTransport)
                .Include(a => a.OperationalOrder)
                    .ThenInclude(po => po.CostCenter)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(p => p.Section)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(p => p.LotPosition)
                .Include(a => a.AssignmentStockPlacements)
                    .ThenInclude(p => p.RackPosition)
                .Include(a => a.Codes)
                .FirstOrDefaultAsync(a => a.Id == placement.AssignmentId, cancellationToken);

            if (assignment is null)
            {
                return _errorManager.ThrowBadRequest<PositionDetailDto>(
                    "No se encontró la asignación asociada a la posición.", "ERP:ASSIGNMENT_NOT_FOUND");
            }

            var remainingPositions = assignment.AssignmentStockPlacements
                .Where(p => p.Id != placement.Id)
                .ToList();

            var qrCode = assignment.Codes.FirstOrDefault(c => c.CodeType == CodesType.Qr);
            var barCode = assignment.Codes.FirstOrDefault(c => c.CodeType == CodesType.Bar);
            var additionalData = DeserializeAdditionalData(assignment.AdditionalData);

            return new PositionDetailDto
            {
                OperationalOrderDetail = _mapper.Map<OperationalOrderDetailsDto>(assignment.OperationalOrder),
                RemainingPositions = _mapper.Map<List<AssignmentStockPlacementsInformation>>(remainingPositions),
                CodeQr = qrCode?.ImageUrl,
                CodeBar = barCode?.ImageUrl,
                QrCode = qrCode?.CodeGenerated,
                BarCode = barCode?.CodeGenerated,
                MerchandiseInformation = new AssignmentMerchandiseInformation
                {
                    Merchandise = assignment.Merchandise,
                    MerchandiseDescription = assignment.MerchandiseDescription,
                    HasMerchandiseDescription = assignment.HasMerchandiseDescription,
                    Category = assignment.Category,
                    MerchandiseType = assignment.MerchandiseType,
                    DestinationType = assignment.DestinationType,
                    Observations = assignment.Observations,
                    HasPositionatingInformation = assignment.HasPositionatingInformation,
                    Pallets = additionalData.PositionatingInformation
                }
            };
        }

        private static AdditionalDataAssingmentOperational DeserializeAdditionalData(string? additionalDataJson)
        {
            if (string.IsNullOrWhiteSpace(additionalDataJson))
            {
                return new AdditionalDataAssingmentOperational();
            }

            var data = JsonSerializer.Deserialize<AdditionalDataAssingmentOperational>(
                additionalDataJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })
                ?? new AdditionalDataAssingmentOperational();

            data.PositionatingInformation ??= [];

            return data;
        }
    }
}