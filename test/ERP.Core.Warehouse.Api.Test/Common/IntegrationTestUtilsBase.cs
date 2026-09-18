using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Warehouse;

namespace ERP.Core.Warehouse.Api.Test.Common
{

    /// <summary>
    /// Todos los metodos reutilizables, puedes agregarlos aqui
    /// </summary>
    public class IntegrationTestUtilsBase : IntegrationTestBase
    {
        public async Task<Guid> CreateUser(string fullname, Guid? areaId = null)
        {
            Guid workAreaId;

            if (areaId.HasValue) workAreaId = areaId.Value;
            else
            {
                var area = await _unitOfWork.WorkAreas.Entities
                    .Where(workArea => workArea.IsActive)
                    .Where(workArea => workArea.WorkAreaCode == 10)
                    .FirstOrDefaultAsync(default);

                workAreaId = area!.Id;
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
                .Where(branch => branch.BranchCode == "ALPAC-01")
                .FirstOrDefaultAsync(default);

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
    }
}