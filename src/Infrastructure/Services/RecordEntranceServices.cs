using ERP.Core.Application.Commons.Interfaces; // Para IIdentityService
using ERP.Core.Warehouse.Api.Application.Commons.Interfaces; // Para IRecordEntranceServices
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Application.Features.ReceptionEntrance.v1.Dtos;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ERP.Core.Warehouse.Api.Infrastructure.Services
{
    public class RecordEntranceServices(
        IUnitOfWork _uniOfWork,
        IHttpContextAccessor _httpContextAccessor,
        IErrorManager _errorManager) : IRecordEntranceServices
    {
        public async Task<GetActiveRecordEntranceResponse> GetActiveRecordEntrance()
        {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userIdString))
            {
                return _errorManager.ThrowBadRequest<GetActiveRecordEntranceResponse>(
                    "No se pudo identificar al usuario en la sesión actual.", "ERP:AUTH_01"
                );
            }

            var activeEntrance
        }
    }
}