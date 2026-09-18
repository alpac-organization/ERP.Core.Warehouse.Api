using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common.Utils;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Warehouse;
using System.Text.Json;

namespace ERP.Core.Warehouse.Api.Test.Common
{
    public class IntegrationTestUtilsBase : IntegrationTestBase
    {
        
        //All companies here for ERP-System!, las demas clases lo heredan la Data structure.
        protected static readonly string[] AllCompanies = ["ALPAC", "AMINSA", "AVASA", "VIGEMSA", "TMN"];  

        // crear usuario Dinámico por compañía
        public async Task<Guid> CreateUser(string fullname, Guid? areaId = null, string companyAlias = "ALPAC")
        {
            var company = await _unitOfWork.Companies.Entities
                .FirstAsync(c => c.Alias == companyAlias);

            Guid workAreaId;
            if (areaId.HasValue) workAreaId = areaId.Value;
            else
            {
                var area = await _unitOfWork.WorkAreas.Entities
                    .Where(w => w.IsActive && w.CompanyId == company.Id && w.WorkAreaCode == 10)
                    .FirstAsync();
                workAreaId = area.Id;
            }

            var newUserId = Guid.NewGuid();
            var suffix = newUserId.ToString("N")[..8];

            await _unitOfWork.Users.CreateNewUser(new()
            {
                Id                   = newUserId,
                UserStatus           = UserStatus.Active,
                UserType             = UserType.StandardUser,
                UserName             = $"user.{suffix}",
                PasswordHash         = "$hashpassword",
                Fullname             = fullname,
                Email                = $"testing.{suffix}@domain.com",
                AreaId               = workAreaId,
                IdentificationNumber = $"001{suffix}A",
            });

            var branch = await _unitOfWork.Branches.Entities
                .Where(b => b.IsActive && b.CompanyId == company.Id)
                .FirstAsync();

            await _unitOfWork.Profiles.CreateNewUserProfile(new()
            {
                Id = Guid.NewGuid(),
                UserId = newUserId,
                BranchId = branch.Id,
                IsActive = true,
                CompanyId = company.Id
            });

            await _unitOfWork.SaveChangesAsync(default);
            return newUserId;
        }

        //  otorgar acceso al modulo 
        public async Task GrantModuleAccessAsync(Guid userId, Guid companyId, RoleType roleType, string moduleCode)
        {
            var profile = await _unitOfWork.Profiles.Entities
                .Where(pr => pr.UserId == userId && pr.CompanyId == companyId)
                .FirstOrDefaultAsync(default);
            
            var role = await _unitOfWork.Roles.Entities
                .Where(r => r.RoleType == roleType)
                .FirstOrDefaultAsync(default);
            
            var module = await _unitOfWork.Modules.Entities
                .Where(m => m.Code == moduleCode)
                .FirstOrDefaultAsync(default);
            
            await _unitOfWork.UserModules.AssignRolesModule(new UserModuleRoles
            {
                Id            = Guid.NewGuid(),
                UserProfileId = profile!.Id,
                RoleId        = role!.Id,
                ModuleId      = module!.Id,
                ModuleCode    = moduleCode,
                IsActive      = true
            });

            await _unitOfWork.SaveChangesAsync(default);
        } 
        //  solicitud base de compra 
        protected async Task<(Guid RequestId, Guid ItemId)> CreateBasePurchaseRequest(
            Guid registeredByUserId,
            Action<PurchaseRequest>? configurePurchaseReq = null)
        {
                var userProfile = await _unitOfWork.Profiles.Entities
                                .FirstAsync(p => p.UserId == registeredByUserId && p.IsActive);

                var user = await _unitOfWork.Users.Entities
                                .FirstAsync(u => u.Id == registeredByUserId);

                var productId = await _unitOfWork.Products.Entities
                                .Select(p => p.Id).FirstAsync();

                var unitMeasureId = await _unitOfWork.UnitsMeasurement.Entities
                                .Select(u => u.Id).FirstAsync();


                var requestId = Guid.NewGuid();
                var itemId = Guid.NewGuid();

                var req = new PurchaseRequest
            {
                Id = requestId,
                IsActive = true,
                Code = $"PR-{requestId}",
                Concept = "Solicitud inicial de compra",
                RequestDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PriorityLevel = PriorityLevel.Normal,
                Destination = DestinationRequest.Internal,
                RequestType = PurchaseRequestType.Requisition,
                RequestStatus = PurchaseRequestStatus.Pending,
                RegisteredByUserId = registeredByUserId,
                BranchId = userProfile.BranchId,
                AreaId = user.AreaId
            }; 

            /* esto permite que el invoke ajuste propiedades específicas que se necesiten para el test
             antes de crear el object,  dando flexibilidad para el testcase. */

            configurePurchaseReq?.Invoke(req);

            await _unitOfWork.PurchaseRequests.RegisterPurchaseRequest(req);

            await _unitOfWork.PurchaseRequestItems.RegisterPurchaseRequestItem(new PurchaseRequestItem
            {
                Id = itemId,
                PurchaseRequestId = requestId,
                ProductId = productId,
                UnitMeasureId = unitMeasureId,
                Quantity = 5,
                QuantityUnit = 1,
                Description = "Ítem inicial",
                Justification = "Justificación inicial",
                AdditionalData = null,
                HasQuotation = false
            });

            await _unitOfWork.SaveChangesAsync(default);
            return (requestId, itemId);
        }

        protected async Task<(Guid CompanyId, Guid UserId, string Token)> ArrangeUserWithRole(
            RoleType roleType, 
            string companyAlias = "ALPAC")
        {
            var company = await _unitOfWork.Companies.Entities
                .FirstAsync(c => c.Alias == companyAlias);
                
            var userId = await CreateUser($"Test User {roleType}", companyAlias: companyAlias);
                    
            await GrantModuleAccessAsync(userId, company.Id, roleType, "COM-129U"); 
                    
            var token = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);
            return (company.Id, userId, token);
        }

        protected static async Task<string?> ReadErrorType(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!TryGetPropertyIgnoreCase(root, "error", out var error))
            return null;

            if (TryGetPropertyIgnoreCase(error, "typeError", out var type) || TryGetPropertyIgnoreCase(error, "type_error", out type))
            {
                return type.GetString();
            }

            return null;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
                if (element.TryGetProperty(name, out value)) return true;
                foreach (var prop in element.EnumerateObject())
                {
                     if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                         value = prop.Value;
                         return true;
                    }
                }
                         value = default;
                         return false;
        }
    }
}