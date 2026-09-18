using System.Text.Json;
using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Warehouse.Api.Test.Common.Utils;

namespace ERP.Core.Warehouse.Api.Test.Common
{

    /// <summary>
    /// Todos los metodos reutilizables, puedes agregarlos aqui
    /// </summary>
    public class IntegrationTestUtilsBase : IntegrationTestBase
    {
        //All companies here for ERP-System!, las demas clases lo heredan la Data structure.
        protected static readonly string[] AllCompanies = ["ALPAC", "AMINSA", "AVASA", "VIGEMSA", "TMN"];

        public async Task<Guid> CreateUser(string fullname, Guid? areaId = null, string companyAlias = "ALPAC")
        {
            var company = await _unitOfWork.Companies.Entities
                .FirstAsync(c => c.Alias == companyAlias);

            Guid workAreaId;

            if (areaId.HasValue) workAreaId = areaId.Value;
            else
            {
                var area = await _unitOfWork.WorkAreas.Entities
                    .Where(workArea => workArea.IsActive)
                    .Where(workArea => workArea.CompanyId == company.Id && workArea.WorkAreaCode == 10)
                    .FirstAsync(default);

                workAreaId = area.Id;
            }

            var newUserId = Guid.NewGuid();

            await _unitOfWork.Users.CreateNewUser(new()
            {
                Id = newUserId,
                UserStatus = UserStatus.Active,
                UserType = UserType.StandardUser,
                UserName = "user.name",
                PasswordHash = "$hashpassoword",
                Fullname = fullname,
                Email = "testing@domain.com",
                AreaId = workAreaId,
                IdentificationNumber = "0010101011052A",
            });

            var branch = await _unitOfWork.Branches.Entities
                .Where(branch => branch.IsActive)
                .Where(branch => branch.CompanyId == company.Id)
                .FirstAsync(default);

            await _unitOfWork.Profiles.CreateNewUserProfile(new()
            {
                Id = Guid.NewGuid(),
                UserId = newUserId,
                BranchId = branch!.Id,
                IsActive = true,
                CompanyId = branch.CompanyId
            });

            var profileId = Guid.NewGuid();

            var role = await _unitOfWork.Roles.Entities
                .FirstOrDefaultAsync(r => r.RoleType == RoleType.Administrator);

            var module = await _unitOfWork.Modules.Entities
                .FirstOrDefaultAsync(m => m.Code == "COM-129U");

            if (role != null)
            {
                await _unitOfWork.UserModules.AssignRolesModule(new UserModuleRoles
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    UserProfileId = profileId,
                    ModuleId = module!.Id,
                    ModuleCode = module.Code,
                    IsActive = true
                });
            }

            await _unitOfWork.SaveChangesAsync(default);

            return newUserId;
        }

        public async Task GrantModuleAccessAsync(Guid userProfileId, string moduleCode)
            => await SetModuleAccessAsync(userProfileId, moduleCode, isActive: true);

        public async Task DenyModuleAccessAsync(Guid userProfileId, string moduleCode)
            => await SetModuleAccessAsync(userProfileId, moduleCode, isActive: false);

        private async Task SetModuleAccessAsync(Guid userProfileId, string moduleCode, bool isActive)
        {
            var role = await _unitOfWork.Roles.Entities
                .FirstAsync(r => r.RoleType == RoleType.Administrator);

            var module = await _unitOfWork.Modules.Entities
                .FirstAsync(m => m.Code == moduleCode);

            await _unitOfWork.UserModules.AssignRolesModule(new UserModuleRoles
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                UserProfileId = userProfileId,
                ModuleId = module.Id,
                ModuleCode = module.Code,
                IsActive = true
            });

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Warehouses> GetOrCreateWarehouseAsync()
        {
            var warehouse = await _unitOfWork.Warehouses.Entities
                .Where(w => w.IsActive && w.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (warehouse is not null) return warehouse;

            warehouse = await _unitOfWork.Warehouses.RegisterWarehouse(new Warehouses
            {
                Id = Guid.NewGuid(),
                Code = "B-Test-01",
                IsActive = true,
                WarehouseType = WarehouseType.Fiscal
            });

            await _unitOfWork.SaveChangesAsync();
            return warehouse;
        }

        public async Task<Sections> GetOrCreateSectionToLotsAsync()
        {
            var section = await _unitOfWork.Sections.Entities
                .Where(s => s.IsActive && s.DeletedAt == null)
                .Where(s => s.SectionStorageType == SectionStorageType.Lots)
                .FirstOrDefaultAsync();

            if (section is not null)
                return section;

            var warehouse = await GetOrCreateWarehouseAsync();

            section = await _unitOfWork.Sections.RegisterSection(new Sections
            {
                Id = Guid.NewGuid(),
                Code = "SEC-Test-01",
                IsActive = true,
                SectionType = SectionType.Storage,
                SectionStorageType = SectionStorageType.Lots,
                WarehouseId = warehouse.Id
            });

            await _unitOfWork.SectionCapacities.RegisterSectionCapacity(new SectionCapacity
            {
                Id = Guid.NewGuid(),
                SectionId = section.Id,
                Width = 8.00m,
                Length = 20.00m,
                UnusedAreaM2 = 16.00m,
                AvailableAreaWithMarginM2 = 120.00m,
                TotalAreaM2 = 160.00m,
                UnoccupiedChargeableAreaM2 = 0m,
                OccupiedChargeableAreaM2 = 0m,
                PercentageAvailableAreaWithMarginM2 = 75.00m
            });

            await _unitOfWork.SaveChangesAsync();
            return section;
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
             antes de crear el object, dando flexibilidad para el testcase. */
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

            var profile = await _unitOfWork.Profiles.Entities
                .FirstAsync(pr => pr.UserId == userId);

            var role = await _unitOfWork.Roles.Entities
                .FirstAsync(r => r.RoleType == roleType);

            var module = await _unitOfWork.Modules.Entities
                .FirstAsync(m => m.Code == "COM-129U");

            await _unitOfWork.UserModules.AssignRolesModule(new UserModuleRoles
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                UserProfileId = profile.Id,
                ModuleId = module.Id,
                ModuleCode = module.Code,
                IsActive = true
            });

            await _unitOfWork.SaveChangesAsync(default);

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