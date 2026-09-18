using NUnit.Framework;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Test.Common.Utils;
using Microsoft.EntityFrameworkCore;
using System.Net;
using ERP.Core.Database.Domain.Enums;
using System.Text.Json;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Entities.Catalogs;

namespace ERP.Core.Warehouse.Api.Test.Controllers
{
   [TestFixture]
   public class SectionControllerTest : IntegrationTestUtilsBase
   {
      // Ruta base de secciones para la compañía, módulo y almacén del test
      private static string SectionBaseUrl(Guid companyId, string moduleCode, Guid warehouseId)
         => $"/api/v1/companies/{companyId}/modules/{moduleCode}/warehouses/{warehouseId}/sections";

      /*
         POST: registra una sección en un almacén que ya tiene capacidad.
      */
      [TestCase("ALPAC", "ALM-MAN-2KE4")]
      public async Task RegisterSectionWhenIsSucess(string companyAlias, string moduleCode)
      {
         // 1. Usuario con acceso de administrador al módulo
         Guid userId = await CreateUser("Carlos Alberto Mendoza Gutiérrez");
         await AssignProfileToModule(userId, moduleCode, RoleType.Administrator);

         var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);

         var company = await _unitOfWork.Companies.Entities
            .Where(c => c.IsActive)
            .Where(c => c.Alias == companyAlias)
            .FirstOrDefaultAsync(default);

         // 2. Almacén y capacidad: el handler exige capacidad registrada
         var warehouse = new Warehouses
         {
            Code = "WH-TEST-01",
            WarehouseType = WarehouseType.Fiscal,
            IsActive = true,
         };

         var warehouseId = await CreateWarehouse(warehouse);

         await CreateWarehouseCapacity(new WarehouseCapacity
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Width = 50,
            Length = 40,
            TotalAreaM2 = 2000,
            UnusedAreaM2 = 0,
            AvailableAreaWithMarginM2 = 2000,
            OccupiedChargeableAreaM2 = 0,
            UnoccupiedChargeableAreaM2 = 2000,
            PercentageAvailableAreaWithMarginM2 = 100,
            HasMargins = false,
            MinimumHeight = 0,
            MaximumHeight = 8,
            MarginTop = 1,
            MarginBottom = 1,
            MarginRight = 1,
            MarginLeft = 1,
            TotalVolumenM3 = 16000,
            UnusedVolumenM3 = 0,
            AvailableVolumenWithMarginM3 = 16000,
            OccupiedChargeableVolumenM3 = 0,
            UnoccupiedChargeableVolumenM3 = 16000,
            PercentageAvailableVolumenWithMarginM3 = 100
         });

         // 3. Body de la petición
         var payload = new Dictionary<string, object>
         {
            ["code"] = "SECTION_001",
            ["section_type"] = 1,
            ["section_storage_type"] = 1,
            ["width"] = 25,
            ["length"] = 25
         };

         var url = SectionBaseUrl(company!.Id, moduleCode, warehouseId);
         var response = await SendRequestAsync(HttpMethod.Post, url, bearerToken, payload);

         Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
      }

      /*
         POST: registra coordenadas de una sección existente.
      */
      [TestCase("ALPAC", "ALM-MAN-2KE4")]
      public async Task RegisterSectionCoordinatesWhenIsSuccess(string companyAlias, string moduleCode)
      {
         // 1. Usuario con acceso de administrador al módulo
         Guid userId = await CreateUser("Carlos Alberto Mendoza Gutiérrez");
         await AssignProfileToModule(userId, moduleCode, RoleType.Administrator);

         var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);

         var company = await _unitOfWork.Companies.Entities
            .Where(c => c.IsActive)
            .Where(c => c.Alias == companyAlias)
            .FirstOrDefaultAsync(default);

         // 2. Almacén y sección sobre la que se van a guardar coordenadas
         var warehouse = new Warehouses
         {
            Code = "WH-TEST-01",
            WarehouseType = WarehouseType.Fiscal,
            IsActive = true,
         };

         var warehouseId = await CreateWarehouse(warehouse);

         var section = new Sections()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = "SECTION_002",
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Lots
         };

         var sectionId = await CreateSection(section);

         var baseUrl = SectionBaseUrl(company!.Id, moduleCode, warehouseId);
         var url = $"{baseUrl}/{sectionId}/coordinates";

         // 3. Body de la petición
         var payload = new Dictionary<string, object>
         {
            ["position_x"] = 1.0,
            ["position_y"] = 2,
            ["position_z"] = 3,
            ["rotation_y"] = 4
         };

         var response = await SendRequestAsync(HttpMethod.Post, url, bearerToken, payload);
         Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
      }

      /*
         GET: lista secciones paginadas. Cada TestCase cubre un page_size distinto.
      */
      [TestCase("ALPAC", "ALM-MAN-2KE4", 1, 2)]
      [TestCase("ALPAC", "ALM-MAN-2KE4", 1, 4)]
      public async Task GetSectionsWhenIsSuccess(string companyAlias, string moduleCode, int pageNumber, int pageSize)
      {
         // 1. Usuario con acceso de administrador al módulo
         Guid userId = await CreateUser("Carlos Alberto Mendoza Gutiérrez");
         await AssignProfileToModule(userId, moduleCode, RoleType.Administrator);

         var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);

         var company = await _unitOfWork.Companies.Entities
            .Where(c => c.IsActive)
            .Where(c => c.Alias == companyAlias)
            .FirstOrDefaultAsync(default);

         // 2. Almacén con cuatro secciones para poder paginar
         var warehouse = new Warehouses()
         {
            Id = Guid.NewGuid(),
            Code = "WH-TEST-01",
            IsActive = true,
            WarehouseType = WarehouseType.Fiscal
         };

         var warehouseId = await CreateWarehouse(warehouse);

         await CreateSection(new()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = "SECTION_001",
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Lots
         });

         await CreateSection(new()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = "SECTION_002",
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Lots
         });

         await CreateSection(new()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = "SECTION_003",
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Racks
         });

         await CreateSection(new()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = "SECTION_004",
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Racks
         });

         var baseUrl = SectionBaseUrl(company!.Id, moduleCode, warehouseId);
         var url = $"{baseUrl}?page_size={pageSize}&page_number={pageNumber}";

         var response = await SendRequestAsync(HttpMethod.Get, url, bearerToken);

         Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

         // 3. El arreglo data debe traer tantos elementos como page_size
         var json = await response.Content.ReadAsStringAsync();
         using var doc = JsonDocument.Parse(json);

         Assert.That(doc.RootElement.GetProperty("data").GetArrayLength(), Is.EqualTo(pageSize));
      }

      /*
         GET: detalle de una sección, incluyendo capacidad y coordenadas.
      */
      [TestCase("ALPAC", "ALM-MAN-2KE4")]
      public async Task GetSectionDetailsWhenIsSuccess(string companyAlias, string moduleCode)
      {
         // 1. Usuario con acceso de administrador al módulo
         Guid userId = await CreateUser("Carlos Alberto Mendoza Gutiérrez");
         await AssignProfileToModule(userId, moduleCode, RoleType.Administrator);

         var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);

         var company = await _unitOfWork.Companies.Entities
            .Where(c => c.IsActive)
            .Where(c => c.Alias == companyAlias)
            .FirstOrDefaultAsync(default);

         // 2. Almacén, sección, capacidad y coordenadas (el detalle las incluye)
         var warehouse = new Warehouses()
         {
            Id = Guid.NewGuid(),
            Code = "WH-TEST-01",
            IsActive = true,
            WarehouseType = WarehouseType.Fiscal
         };

         var warehouseId = await CreateWarehouse(warehouse);

         var section = new Sections()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = "SECTION_002",
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Lots
         };

         var sectionId = await CreateSection(section);

         await CreateSectionCapacity(new SectionCapacity
         {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Width = 25,
            Length = 25,
            TotalAreaM2 = 625,
            UnusedAreaM2 = 0,
            AvailableAreaWithMarginM2 = 625,
            OccupiedChargeableAreaM2 = 0,
            UnoccupiedChargeableAreaM2 = 625,
            PercentageAvailableAreaWithMarginM2 = 100
         });

         await CreateSectionCoordinates(new SectionCoordinates
         {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            PositionX = 1.0m,
            PositionY = 1.0m,
            PositionZ = 1.0m,
            RotationY = 1.0m
         });

         var baseUrl = SectionBaseUrl(company!.Id, moduleCode, warehouseId);
         var url = $"{baseUrl}/{sectionId}/details";

         var response = await SendRequestAsync(HttpMethod.Get, url, bearerToken);

         Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

         // 3. La respuesta debe traer la estructura del detalle
         var json = await response.Content.ReadAsStringAsync();
         using var doc = JsonDocument.Parse(json);

         var root = doc.RootElement;

         Assert.That(root.TryGetProperty("section_id", out _), Is.True);
         Assert.That(root.TryGetProperty("section_code", out _), Is.True);
         Assert.That(root.TryGetProperty("is_active", out _), Is.True);
         Assert.That(root.TryGetProperty("capacity", out _), Is.True);
         Assert.That(root.TryGetProperty("coordinates", out _), Is.True);
      }

      /*
         PATCH: actualiza código, ancho y largo. Se compara el estado anterior contra el persistido.
      */
      [TestCase("ALPAC", "ALM-MAN-2KE4")]
      public async Task UpdateSectionWhenIsSuccess(string companyAlias, string moduleCode)
      {
         // 1. Usuario con acceso de administrador al módulo
         Guid userId = await CreateUser("Carlos Alberto Mendoza Gutiérrez");
         await AssignProfileToModule(userId, moduleCode, RoleType.Administrator);

         // Valores de antes y después para contrastar el update
         var testData = new
         {
            sectionCodeBefore = "SECTION_001",
            widthBefore = 25,
            lengthBefore = 25,
            sectionCodeAfter = "UPDATED_SECTION_002",
            widthAfter = 100,
            lengthAfter = 100
         };

         var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);

         var company = await _unitOfWork.Companies.Entities
            .Where(c => c.IsActive)
            .Where(c => c.Alias == companyAlias)
            .FirstOrDefaultAsync(default);

         // 2. Almacén, capacidad de almacén y sección con su capacidad (el handler las exige)
         var warehouse = new Warehouses
         {
            Code = "WH-TEST-01",
            WarehouseType = WarehouseType.Fiscal,
            IsActive = true,
         };

         var warehouseId = await CreateWarehouse(warehouse);

         await CreateWarehouseCapacity(new WarehouseCapacity
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Width = 50,
            Length = 40,
            TotalAreaM2 = 2000,
            UnusedAreaM2 = 0,
            AvailableAreaWithMarginM2 = 2000,
            OccupiedChargeableAreaM2 = 0,
            UnoccupiedChargeableAreaM2 = 2000,
            PercentageAvailableAreaWithMarginM2 = 100,
            HasMargins = false,
            MinimumHeight = 0,
            MaximumHeight = 8,
            MarginTop = 1,
            MarginBottom = 1,
            MarginRight = 1,
            MarginLeft = 1,
            TotalVolumenM3 = 16000,
            UnusedVolumenM3 = 0,
            AvailableVolumenWithMarginM3 = 16000,
            OccupiedChargeableVolumenM3 = 0,
            UnoccupiedChargeableVolumenM3 = 16000,
            PercentageAvailableVolumenWithMarginM3 = 100
         });

         var section = new Sections()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = testData.sectionCodeBefore,
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Lots
         };

         var sectionId = await CreateSection(section);

         await CreateSectionCapacity(new SectionCapacity
         {
            Id = Guid.NewGuid(),
            SectionId = sectionId,
            Width = testData.widthBefore,
            Length = testData.lengthBefore,
            TotalAreaM2 = 625,
            UnusedAreaM2 = 0,
            AvailableAreaWithMarginM2 = 625,
            OccupiedChargeableAreaM2 = 0,
            UnoccupiedChargeableAreaM2 = 625,
            PercentageAvailableAreaWithMarginM2 = 100
         });

         // 3. Body de la petición
         var payload = new Dictionary<string, object>
         {
            ["code"] = testData.sectionCodeAfter,
            ["width"] = testData.widthAfter,
            ["length"] = testData.lengthAfter
         };

         var baseUrl = SectionBaseUrl(company!.Id, moduleCode, warehouseId);
         var url = $"{baseUrl}/{sectionId}";

         var response = await SendRequestAsync(HttpMethod.Patch, url, bearerToken, payload);

         // 4. Lectura del registro actualizado (AsNoTracking para no devolver la instancia en memoria)
         var updatedSection = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .Include(s => s.SectionCapacity)
            .FirstOrDefaultAsync(s => s.Id == sectionId, default);

         Assert.That(updatedSection, Is.Not.Null);
         Assert.That(updatedSection!.SectionCapacity, Is.Not.Null);

         // 5. Los datos ya no coinciden con los de antes
         Assert.That(updatedSection.Code, Is.Not.EqualTo(testData.sectionCodeBefore));
         Assert.That(updatedSection.SectionCapacity!.Width, Is.Not.EqualTo(testData.widthBefore));
         Assert.That(updatedSection.SectionCapacity.Length, Is.Not.EqualTo(testData.lengthBefore));

         // 6. Quedaron con los valores enviados en el PATCH
         Assert.That(updatedSection.Code, Is.EqualTo(testData.sectionCodeAfter));
         Assert.That(updatedSection.SectionCapacity.Width, Is.EqualTo(testData.widthAfter));
         Assert.That(updatedSection.SectionCapacity.Length, Is.EqualTo(testData.lengthAfter));
      }

      /*
         DELETE: soft delete. La fila sigue existiendo con IsActive = false y DeletedAt informado.
      */
      [TestCase("ALPAC", "ALM-MAN-2KE4")]
      public async Task DeleteSectionWhenIsSuccess(string companyAlias, string moduleCode)
      {
         // 1. Usuario con acceso de administrador al módulo
         Guid userId = await CreateUser("Carlos Alberto Mendoza Gutiérrez");
         await AssignProfileToModule(userId, moduleCode, RoleType.Administrator);

         var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);

         var company = await _unitOfWork.Companies.Entities
            .Where(c => c.IsActive)
            .Where(c => c.Alias == companyAlias)
            .FirstOrDefaultAsync(default);

         // 2. Almacén y sección a eliminar
         var warehouse = new Warehouses
         {
            Code = "WH-TEST-01",
            WarehouseType = WarehouseType.Fiscal,
            IsActive = true,
         };

         var warehouseId = await CreateWarehouse(warehouse);

         var section = new Sections()
         {
            Id = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Code = "SECTION_002",
            IsActive = true,
            SectionType = SectionType.Storage,
            SectionStorageType = SectionStorageType.Lots
         };

         var sectionId = await CreateSection(section);

         var baseUrl = SectionBaseUrl(company!.Id, moduleCode, warehouseId);
         var url = $"{baseUrl}/{sectionId}";

         var response = await SendRequestAsync(HttpMethod.Delete, url, bearerToken);

         Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

         // 3. Relectura en BD: no debe devolver la instancia trackeada del arrange
         var deletedSection = await _unitOfWork.Sections.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sectionId, default);

         Assert.That(deletedSection, Is.Not.Null);
         Assert.That(deletedSection!.IsActive, Is.False);
         Assert.That(deletedSection!.DeletedAt, Is.Not.Null);
      }
   }
}
