using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;

namespace ERP.Core.Warehouse.Api.Test.Controllers.purchasing
{
    [TestFixture]
    public class UpdatePurchaseRequestControllerTest : IntegrationTestUtilsBase
    {
        private const string ModuleCode = "COM-129U";
        private static string UpdatePurchaseUrl(Guid companyId, Guid purchaseRequestId) => 
            $"/api/v1/companies/{companyId}/modules/{ModuleCode}/purchase-requests/{purchaseRequestId}";
       private static async Task<string?> ReadErrorType(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!TryGetPropertyIgnoreCase(root, "error", out var error))
                return null;

            if (TryGetPropertyIgnoreCase(error, "typeError", out var type) ||
                TryGetPropertyIgnoreCase(error, "type_error", out type))
            {
                return type.GetString();
            }

            return null;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
        {
            if (element.TryGetProperty(name, out value))
                return true;

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


        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseHeaderAndItem(string companyAlias)
        {
            // Arrange
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, itemId) = await SeedPurchaseRequestAsync(userId);

            var payload = new
            {
                observations = "Cabecera actualizada",
                priority_level = PriorityLevel.Critical,
                purchase_request_items = new[]
                {
                    new { id = itemId, quantity = 15, description = "Ítem actualizado" }
                }
            };

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var purchase = await _unitOfWork.PurchaseRequests.Entities
                .AsNoTracking()
                .Include(p => p.PurchaseRequestItems)
                .FirstAsync(p => p.Id == requestId);

            purchase.Concept.Should().Be("Cabecera actualizada");
            purchase.PriorityLevel.Should().Be(PriorityLevel.Critical);
            purchase.PurchaseRequestItems.First().Quantity.Should().Be(15);
            purchase.PurchaseRequestItems.First().Description.Should().Be("Ítem actualizado");
        }

        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseImages(string companyAlias)
        {
            // Arrange
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, itemId) = await SeedPurchaseRequestAsync(userId);

            var payload = new
            {
                purchase_request_items = new[]
                {
                    new { id = itemId, images_product_to_changed = new[] { "https://img1.png" } }
                }
            };

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var item = await _unitOfWork.PurchaseRequestItems.Entities.AsNoTracking().FirstAsync(i => i.Id == itemId);
            item.AdditionalData.Should().Contain("img1.png");
        }


        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseAsSupervisor(string companyAlias)
        {
            //Supervisor no puede actualizar
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Supervisor, companyAlias);
            var (requestId, _) = await SeedPurchaseRequestAsync(userId);

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, new { observations = "test" });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorType = await ReadErrorType(response);
            errorType.Should().Be("ERP:INVALID_ACCESS");
        }

        // test: Estados invalidos 

        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseNotFoundOrInactive(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await SeedPurchaseRequestAsync(userId, isActive: false); // Inactiva

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, new { observations = "test" });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var errorType = await ReadErrorType(response);
            errorType.Should().Be("ERP:QUOTATION_NOT_FOUND");
        }

        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseAsNotPending(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await SeedPurchaseRequestAsync(userId, status: PurchaseRequestStatus.Approved); // Ya aprobada

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, new { observations = "test" });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorType = await ReadErrorType(response);
            errorType.Should().Be("ERP:PURCHASE_REQUEST_NOT_PENDING");
        }

        // test: Reglas de prioridad
        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseAsInvalidType(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await SeedPurchaseRequestAsync(userId, requestType: PurchaseRequestType.Requisition);

            var body = new { priority_level = PriorityLevel.None }; 

            // Requisición no puede ser tipo None
            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token,body); 

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorType = await ReadErrorType(response);
            errorType.Should().Be("ERP:INVALID_PRIORITY");
        }

        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseEventual(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await SeedPurchaseRequestAsync(userId, requestType: PurchaseRequestType.Eventual, priority: PriorityLevel.None);

            var body = new { priority_level = PriorityLevel.Critical }; 
            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, 
                body); // Eventual debe ser None de tipo (Ninguna)

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var errorType = await ReadErrorType(response);
            errorType.Should().Be("ERP:INVALID_PRIORITY");
        }

        //test: Validacion de items de la solicitud de compra 

        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseWithItemIdNotFound(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await SeedPurchaseRequestAsync(userId);

            var payload = new { purchase_request_items = new[] { new { id = Guid.NewGuid(), quantity = 10 } } }; // ID Falso

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var errorType = await ReadErrorType(response);
            errorType.Should().Be("ERP:PURCHASE_REQUEST_ITEM_NOT_FOUND");
        }

        [Test]
        [TestCase("ALPAC")]
        public async Task UpdatePurchaseWithQuantityZero(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, itemId) = await SeedPurchaseRequestAsync(userId);

            var payload = new { purchase_request_items = new[] { new { id = itemId, quantity = 0 } } }; // Cantidad 0

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}