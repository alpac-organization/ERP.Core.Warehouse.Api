using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Entities.Catalogs;

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

            await _unitOfWork.SaveChangesAsync(default);
            
            return newUserId;
        }

        /*
            Asigna el perfil del usuario a un módulo con el rol indicado.
            Crea el módulo de prueba y lo vincula al perfil para pasar la validación de acceso.
        */
        public async Task AssignProfileToModule(Guid userId, string moduleCode, RoleType roleType)
        {
            var profile = await _unitOfWork.Profiles.Entities
                .FirstOrDefaultAsync(p => p.UserId == userId, default);

            var role = await _unitOfWork.Roles.Entities
                .FirstOrDefaultAsync(r => r.RoleType == roleType, default);

            var moduleId = Guid.NewGuid();

            await _unitOfWork.Modules.CreateModuleAssociatedWithCompany(new()
            {
                Id = moduleId,
                Code = moduleCode,
                Description = "",
                IsActive = true,
                ModuleName = "Modulo de Prueba",
            }, default);

            await _unitOfWork.UserModules.AssignRolesModule(new()
            {
                UserProfileId = profile!.Id,
                ModuleId = moduleId,
                RoleId = role!.Id,
                ModuleCode = moduleCode,
                IsActive = true,
            });

            await _unitOfWork.SaveChangesAsync(default);
        }

        /*
            Persiste un almacén de prueba. El payload debe traer Id, código y tipo.
        */
        public async Task<Guid> CreateWarehouse(Warehouses warehouse)
        {
            var register = await _unitOfWork.Warehouses.RegisterWarehouse(warehouse);

            await _unitOfWork.SaveChangesAsync(default);

            return register.Id;
        }

        /*
            Persiste la capacidad de un almacén. El payload debe traer Id y WarehouseId.
        */
        public async Task<Guid> CreateWarehouseCapacity(WarehouseCapacity warehouseCapacity)
        {
            var register = await _unitOfWork.WarehouseCapacities.RegisterWarehouseCapacity(warehouseCapacity);

            await _unitOfWork.SaveChangesAsync(default);

            return register.Id;
        }

        /*
            Persiste una sección de prueba. El payload debe traer Id y WarehouseId.
        */
        public async Task<Guid> CreateSection(Sections section)
        {
            var register = await _unitOfWork.Sections.RegisterSection(section);

            await _unitOfWork.SaveChangesAsync(default);

            return register.Id;
        }

        /*
            Persiste las coordenadas de una sección. El payload debe traer Id y SectionId.
        */
        public async Task<Guid> CreateSectionCoordinates(SectionCoordinates sectionCoordinates)
        {
            var register = await _unitOfWork.SectionCoordinates.RegisterSectionCoordinates(sectionCoordinates);

            await _unitOfWork.SaveChangesAsync(default);

            return register.Id;
        }

        /*
            Persiste la capacidad de una sección. El payload debe traer Id y SectionId.
        */
        public async Task<Guid> CreateSectionCapacity(SectionCapacity sectionCapacity)
        {
            var register = await _unitOfWork.SectionCapacities.RegisterSectionCapacity(sectionCapacity);

            await _unitOfWork.SaveChangesAsync(default);

            return register.Id;
        }
    }
}