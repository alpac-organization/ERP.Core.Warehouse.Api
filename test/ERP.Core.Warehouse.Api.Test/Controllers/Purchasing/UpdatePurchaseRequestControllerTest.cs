using System.Net;
using System.Text.Json;
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

        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseHeaderAndItem(string companyAlias)
        {
            // Arrange
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, itemId) = await CreateBasePurchaseRequest(userId);

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

            // Assert aqui
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var purchase = await _unitOfWork.PurchaseRequests.Entities
                .AsNoTracking()
                .Include(p => p.PurchaseRequestItems)
                .FirstAsync(p => p.Id == requestId);

            var updatedItem = purchase.PurchaseRequestItems.First();

            Assert.Multiple(() =>
            {
                Assert.That(purchase.Concept, Is.EqualTo("Cabecera actualizada"));
                Assert.That(purchase.PriorityLevel, Is.EqualTo(PriorityLevel.Critical));
                Assert.That(updatedItem.Quantity, Is.EqualTo(15));
                Assert.That(updatedItem.Description, Is.EqualTo("Ítem actualizado"));
            });
        }

        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseImages(string companyAlias)
        {
            // Arrange
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, itemId) = await CreateBasePurchaseRequest(userId);

            var payload = new
            {
                purchase_request_items = new[]
                {
                    new { id = itemId, images_product_to_changed = new[] { "https://imgtesting.png" } }
                }
            };

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var item = await _unitOfWork.PurchaseRequestItems.Entities
                      .AsNoTracking()
                      .FirstAsync(i => i.Id == itemId);
            
            Assert.That(item.AdditionalData, Does.Contain("imgtesting.png"));
        }


        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseAsSupervisor(string companyAlias)
        {
            //Supervisor no puede actualizar
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Supervisor, companyAlias);
            var (requestId, _) = await CreateBasePurchaseRequest (userId);

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, new { observations = "test" });
            var errorType = await ReadErrorType(response);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(errorType, Is.EqualTo("ERP:INVALID_ACCESS"));
            });
        }

        // test: Estados invalidos 
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseNotFoundOrInactive(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);

            // aqui este usa el configure Invoke para setear a Inactiva
            var (requestId, _) = await CreateBasePurchaseRequest(userId, req => req.IsActive = false); 

            var body =  new { observations = "test" };

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, body);
            var errorType = await ReadErrorType(response);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(errorType, Is.EqualTo("ERP:QUOTATION_NOT_FOUND"));
            });
        }

        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseAsNotPending(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            
            // volvemos a usar el configure para setear Approved la purchase.
            var (requestId, _) = await CreateBasePurchaseRequest(userId, req=> req.RequestStatus = PurchaseRequestStatus.Approved); 

            
            var payload = new { observations = "test" };
            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload );
            var errorType = await ReadErrorType(response);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(errorType, Is.EqualTo("ERP:PURCHASE_REQUEST_NOT_PENDING"));
            });
        }

        // test: Reglas de prioridad
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseAsInvalidType(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await CreateBasePurchaseRequest(userId, req=> req.RequestType = PurchaseRequestType.Requisition);

            var body = new { priority_level = PriorityLevel.None }; 

            // Requisición no puede ser tipo None
            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token,body); 
            var errorType = await ReadErrorType(response);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(errorType, Is.EqualTo("ERP:INVALID_PRIORITY"));
            });
        }

        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseEventual(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await CreateBasePurchaseRequest(userId, req =>
            {
                req.RequestType =  PurchaseRequestType.Eventual;
                req.PriorityLevel =  PriorityLevel.None;
            });

            var body = new { priority_level = PriorityLevel.Critical}; 

            // Eventual debe ser None de tipo (Ninguna)
            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token,body); 
            var errorType = await ReadErrorType(response);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(errorType, Is.EqualTo("ERP:INVALID_PRIORITY"));
            });
        }

        //test: Validacion de items de la solicitud de compra 

        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseWithItemIdNotFound(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, _) = await CreateBasePurchaseRequest(userId);

            // testeando ID Falso
            var payload = new { purchase_request_items = new[] { new { id = Guid.NewGuid(), quantity = 10 } } }; 

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload);
            var errorType = await ReadErrorType(response);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(errorType, Is.EqualTo("ERP:PURCHASE_REQUEST_ITEM_NOT_FOUND"));
            });
        }

        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task UpdatePurchaseWithQuantityZero(string companyAlias)
        {
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var (requestId, itemId) = await CreateBasePurchaseRequest(userId);

            var payload = new { purchase_request_items = new[] { new { id = itemId, quantity = 0 } } }; // Cantidad 0

            var response = await SendRequestAsync(HttpMethod.Patch, UpdatePurchaseUrl(companyId, requestId), token, payload);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}