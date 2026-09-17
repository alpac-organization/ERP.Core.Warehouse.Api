using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common.Utils;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Warehouse;

namespace ERP.Core.Warehouse.Api.Test.Common
{
    public class IntegrationTestUtilsBase : IntegrationTestBase
    {
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
                Id = newUserId,
                UserStatus = UserStatus.Active,
                UserType = UserType.StandardUser,
                UserName = $"user.{suffix}",
                PasswordHash = "$hashpassword",
                Fullname = fullname,
                Email = $"testing.{suffix}@domain.com",
                AreaId = workAreaId,
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
                Id = Guid.NewGuid(),
                UserProfileId = profile!.Id,
                RoleId = role!.Id,
                ModuleId = module!.Id,
                ModuleCode = moduleCode,
                IsActive = true
            });

            await _unitOfWork.SaveChangesAsync(default);
        }

        //  Crear catalogo  
        public async Task<(List<Guid> ProductIds, List<Guid> MeasureIds)> SeedCatalogForPurchase(
            int productCount = 1, 
            int measureCount = 1)
        {
            // 1. Crear algunas categorías variadas
            var categoryIds = new List<Guid>();
            for (int i = 0; i < Math.Max(1, productCount / 2); i++) // Crea 1 categoría por cada 2 productos
            {
                var category = await _unitOfWork.CategoryProducts.CreateCategoryProduct(new CategoryProducts
                {
                    Id = Guid.NewGuid(),
                    Name = $"Categoría test {i}",
                    Code = $"CAT-{i}",
                    IsActive = true
                });
                categoryIds.Add(category.Id);
            }

            //  Crear unidades de medida variadas
            var measureIds = new List<Guid>();
            var measureTypes = Enum.GetValues<UnitMeasureType>(); // Obtiene todos los tipos 

            for (int i = 0; i < measureCount; i++)
            {
                // Rota entre los diferentes tipos de medida
                var type = measureTypes[i % measureTypes.Length]; 

                var unit = await _unitOfWork.UnitsMeasurement.RegisterUnitMeasure(new UnitMeasure
                {
                    Id = Guid.NewGuid(),
                    Code = $"U-{i}",
                    Name = $"Medida {type} {i}",
                    Symbol = $"s{i}",
                    Description = $"Unidad de prueba tipo {type}",
                    Type = type,
                    IsActive = true
                });
                measureIds.Add(unit.Id);
            }

            //  Crear productos asignándoles categorías variadas
            var productIds = new List<Guid>();
            for (int i = 0; i < productCount; i++)
            {
                var product = await _unitOfWork.Products.InsertProduct(new Product
                {
                    Id = Guid.NewGuid(),
                    ProductName = $"Producto test {i}",
                    Description = "Producto para integration test",
                    CategoryId = categoryIds[i % categoryIds.Count] // Rota entre las categorías creadas
                });
                productIds.Add(product.Id);
            }

            await _unitOfWork.SaveChangesAsync(default);

            return (productIds, measureIds);
        }

        //  solicitud  base de compra 
        protected async Task<(Guid RequestId, Guid ItemId)> SeedPurchaseRequestAsync(
            Guid registeredByUserId,
            PurchaseRequestType requestType = PurchaseRequestType.Requisition,
            PurchaseRequestStatus status = PurchaseRequestStatus.Pending,
            bool isActive = true,
            PriorityLevel priority = PriorityLevel.Normal,
            DestinationRequest destination = DestinationRequest.Internal,
            string? concept = "Solicitud inicial")
        {
            var userProfile = await _unitOfWork.Profiles.Entities
                .FirstAsync(p => p.UserId == registeredByUserId && p.IsActive);

            var user = await _unitOfWork.Users.Entities
                .FirstAsync(u => u.Id == registeredByUserId);

            var (productIds, unitMeasureIds) = await SeedCatalogForPurchase();
            var productId = productIds.First();
            var unitMeasureId = unitMeasureIds.First();

            var requestId = Guid.NewGuid();
            var itemId = Guid.NewGuid();

            await _unitOfWork.PurchaseRequests.RegisterPurchaseRequest(new PurchaseRequest
            {
                Id = requestId,
                IsActive = isActive,
                Code = $"PR-{requestId.ToString("N")[..8]}",
                Concept = concept,
                RequestDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PriorityLevel = priority,
                Destination = destination,
                RequestType = requestType,
                RequestStatus = status,
                RegisteredByUserId = registeredByUserId,
                BranchId = userProfile.BranchId,
                AreaId = user.AreaId
            });

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
    }
}