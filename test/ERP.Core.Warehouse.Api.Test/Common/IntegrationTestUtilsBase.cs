using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Domain.Entities.Shopping;
using ERP.Core.Database.Domain.Entities.Catalogs;
using ERP.Core.Database.Domain.Entities.Accounting;
using ERP.Core.Warehouse.Api.Test.Common.Utils;
using System.Text.Json;
using ERP.Core.Database.Domain.Entities.Warehouse;

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
                Id = newUserId,
                UserStatus = UserStatus.Active,
                UserType = UserType.StandardUser,
                UserName = $"user.{suffix}",
                PasswordHash = "$hashpassword",
                Fullname = fullname,
                Email = $"testing.{suffix}@domain.com",
                IdentificationNumber = $"001{suffix}A",
            });

            var branch = await _unitOfWork.Branches.Entities
                .Where(b => b.IsActive && b.CompanyId == company.Id)
                .FirstAsync();

            var profileId = Guid.NewGuid();

            await _unitOfWork.Profiles.CreateNewUserProfile(new()
            {
                Id = profileId,
                UserId = newUserId,
                BranchId = branch.Id,
                IsActive = true,
                CompanyId = company.Id,
                CostCenterId = Guid.Parse("ffffffff-0000-0000-0000-000000000001")
            });

            await _unitOfWork.SaveChangesAsync(default);
            return newUserId;
        }

        #region Warehouse
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

        #endregion

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

        //  solicitud base de compra 
        protected async Task<(Guid RequestId, Guid ItemId)> CreateBasePurchaseRequest(Guid registeredByUserId, Action<PurchaseRequest>? configurePurchaseReq = null)
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
                AreaId = Guid.Parse("11111111-0000-0000-0000-000000000001")
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
            string companyAlias = "ALPAC",
            string moduleCode = "COM-129U")
        {
            var company = await _unitOfWork.Companies.Entities
                .Where(c => c.IsActive)
                .FirstAsync(c => c.Alias == companyAlias);

            var userId = await CreateUser($"Test User {roleType}", companyAlias: companyAlias);

            await GrantModuleAccessAsync(userId, company.Id, roleType, moduleCode);

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

        #region warehouse

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

        #endregion
        #region Extensiones de Soporte para Flujos de Aprobación, Anulación y Reactivación

        /// Crea una solicitud de compra base vinculada a un usuario registrado, estableciendo su estado
        /// y opcionalmente sembrando dos cotizaciones iniciales para simular un proceso en marcha.
        public async Task<Guid> CreatePurchaseRequest(
            Guid userId, 
            PurchaseRequestStatus status = PurchaseRequestStatus.Approved,
            bool withQuotations = true)
        {
            var (requestId, itemId) = await CreateBasePurchaseRequest(userId, req =>
            {
                req.RequestStatus = status;
                req.Destination = Enum.GetValues<DestinationRequest>().First();
                req.PriorityLevel = PriorityLevel.None;
                req.Concept = "Solicitud de prueba";
            });

            if (withQuotations)
            {
                var supplier = await GetOrCreateSupplierAsync(userId);

                var quote1 = new Quotation
                {
                    Id = Guid.NewGuid(),
                    PurchaseRequestItemId = itemId,
                    SupplierId = supplier.Id,
                    Price = 100m,
                    PriceUnit = 10m,
                    PriceTotal = 100m,
                    Iva = 0m,
                    IsActive = true,
                    IsAcceptedForPurchase = true,
                    QuoteDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    BrandProduct = "Marca A",
                    DeletedAt = null
                };

                var quote2 = new Quotation
                {
                    Id = Guid.NewGuid(),
                    PurchaseRequestItemId = itemId,
                    SupplierId = supplier.Id,
                    Price = 150m,
                    PriceUnit = 15m,
                    PriceTotal = 150m,
                    Iva = 0m,
                    IsActive = true,
                    IsAcceptedForPurchase = false,
                    QuoteDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    BrandProduct = "Marca B",
                    DeletedAt = null
                };

                await _unitOfWork.Quotations.RegisterQuotation(quote1);
                await _unitOfWork.Quotations.RegisterQuotation(quote2);

                var item = await _unitOfWork.PurchaseRequestItems.Entities
                    .FirstAsync(i => i.Id == itemId);
                item.HasQuotation = true;
                await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);

                await _unitOfWork.SaveChangesAsync(default);
            }

            return requestId;
        }

        /// Obtiene un proveedor activo existente o crea uno nuevo de prueba si no existe ninguno en la base de datos.
        public async Task<Supplier> GetOrCreateSupplierAsync(Guid? userId = null)
        {
            var supplier = await _unitOfWork.Suppliers.Entities
                .FirstOrDefaultAsync();

            if (supplier == null)
            {
                var targetUserId = userId.HasValue && userId.Value != Guid.Empty
                    ? userId.Value
                    : await _unitOfWork.Users.Entities.Select(u => u.Id).FirstOrDefaultAsync();

                supplier = new Supplier
                {
                    Id = Guid.NewGuid(),
                    SuppliersLegalName = "Proveedor de Prueba S.A.",
                    CommercialName = "Proveedor Prueba",
                    IdentificationNumber = "0999999999001",
                    ConstitutionType = ConstitutionType.Legal,
                    IdentificationType = IdentificationType.Ruc,
                    UserId = targetUserId,
                    IsActive = true
                };
                await _unitOfWork.Suppliers.RegisterSupplier(supplier);
                await _unitOfWork.SaveChangesAsync(default);
            }

            return supplier;
        }

        /// Registra una revisión contable vinculada a la solicitud de compra especificada.
        public async Task<Guid> CreateAccountingReview(Guid purchaseRequestId, Guid userId, AccountingReviewStatus status = AccountingReviewStatus.Pending)
        {
            var accountingReview = new PurchaseRequestsReviewedAccounting
            {
                Id = Guid.NewGuid(),
                PurchaseRequestId = purchaseRequestId,
                Status = status,
                SentByUserId = userId,
                SentToReviewAt = DateOnly.FromDateTime(DateTime.UtcNow),
                Comments = "Revisión contable de prueba",
                DeletedAt = null
            };

            await _unitOfWork.PurchaseRequestsReviewedAccounting.RegisterPurchaseRequestsReviewedAccounting(accountingReview);
            await _unitOfWork.SaveChangesAsync(default);

            return accountingReview.Id;
        }

        /// Registra una revisión de gerencia vinculada a la solicitud de compra especificada.
        public async Task<Guid> CreateManagementReview(Guid purchaseRequestId, Guid userId, ManagementReviewStatus status = ManagementReviewStatus.Pending)
        {
            var managementReview = new PurchaseRequestsReviewedManagement
            {
                Id = Guid.NewGuid(),
                PurchaseRequestId = purchaseRequestId,
                Status = status,
                SentByUserId = userId,
                SentToReviewAt = DateOnly.FromDateTime(DateTime.UtcNow),
                Comments = "Revisión de gerencia de prueba",
                DeletedAt = null
            };

            await _unitOfWork.PurchaseRequestsReviewedManagement.RegisterRequisitionManagementReview(managementReview);
            await _unitOfWork.SaveChangesAsync(default);

            return managementReview.Id;
        }

        /// Crea una orden de compra vinculada a la solicitud de compra para simular solicitudes que ya fueron emitidas.
        public async Task<Guid> CreatePurchaseOrder(Guid purchaseRequestId, Guid userId)
        {
            var purchaseOrder = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                IsActive = true,
                Comments = "Orden de compra de prueba",
                SentToReviewAt = DateOnly.FromDateTime(DateTime.UtcNow),
                SentByUserId = userId,
                ReviewedByUserId = userId,
                PurchaseRequestId = purchaseRequestId
            };

            await _unitOfWork.PurchaseOrders.RegisterPurchaseOrder(purchaseOrder);
            await _unitOfWork.SaveChangesAsync(default);

            return purchaseOrder.Id;
        }

        /// Agrega una nueva cotización activa a la solicitud de compra y actualiza la bandera HasQuotation en el ítem.
        public async Task<Guid> AddQuotation(Guid purchaseRequestId, bool isAccepted = true)
        {
            var item = await _unitOfWork.PurchaseRequestItems.Entities
                .FirstOrDefaultAsync(i => i.PurchaseRequestId == purchaseRequestId);

            var purchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .FirstOrDefaultAsync(pr => pr.Id == purchaseRequestId);

            var supplier = await GetOrCreateSupplierAsync(purchaseRequest?.RegisteredByUserId);

            var quotation = new Quotation
            {
                Id = Guid.NewGuid(),
                PurchaseRequestItemId = item!.Id,
                SupplierId = supplier.Id,
                Price = 200m,
                PriceUnit = 20m,
                PriceTotal = 200m,
                Iva = 0m,
                IsActive = true,
                IsAcceptedForPurchase = isAccepted,
                QuoteDate = DateOnly.FromDateTime(DateTime.UtcNow),
                BrandProduct = "Marca Nueva",
                DeletedAt = null
            };

            await _unitOfWork.Quotations.RegisterQuotation(quotation);

            item.HasQuotation = true;
            await _unitOfWork.PurchaseRequestItems.UpdateAsync(item);

            await _unitOfWork.SaveChangesAsync(default);

            return quotation.Id;
        }

        #endregion
    }
}