using System.Net;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Application.Features.PurchaseRequests.v1.Commands;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Commands;

namespace ERP.Core.Warehouse.Api.Test.Controllers.purchasing
{
    [TestFixture]
    public class PurchaseRequestAnnulControllerTest : IntegrationTestUtilsBase
    {
        private const string ModuleCode = "COM-129U";

        private static string PurchaseRequestBaseUrl(Guid companyId) =>
            $"/api/v1/companies/{companyId}/modules/{ModuleCode}/purchase-requests";

        private static string AnnulUrl(Guid companyId, Guid purchaseRequestId) =>
            $"/api/v1/companies/{companyId}/modules/{ModuleCode}/purchase-requests/{purchaseRequestId}/annul";

        private static string SendAccountingReviewUrl(Guid companyId, Guid purchaseRequestId) =>
            $"/api/v1/companies/{companyId}/modules/{ModuleCode}/purchase-requests/{purchaseRequestId}/send-accounting-review";

        private static string AnnulAccountingUrl(Guid companyId, Guid reviewId) =>
            $"/api/v1/companies/{companyId}/modules/{ModuleCode}/requisition-accounting-reviews/{reviewId}/annul";

        #region Anulación de Solicitud de Compra

        /// <summary>
        /// Verifica que una solicitud de compra aprobada pueda anularse válidamente,
        /// pasando a estado Rejected, asignando el motivo de anulación, aplicando soft delete
        /// y dando de baja todas las cotizaciones asociadas a sus ítems.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task AnnulPurchaseRequest_WhenValid_ShouldSetRejectedAndInactivateQuotations(string companyAlias)
        {
            // Arrange - Generar usuario operador con acceso al módulo y solicitud aprobada con cotizaciones
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Approved);

            var payload = new AnnulPurchaseRequestCommand
            {
                Reason = "Cancelación presupuestaria por cambio de prioridades operativas."
            };

            // Act - Enviar solicitud HTTP de anulación
            var response = await SendRequestAsync(HttpMethod.Post, AnnulUrl(companyId, purchaseRequestId), token, payload);

            // Assert - Verificar respuesta HTTP 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Assert - Verificar cambios persistidos en base de datos
            var updatedPurchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .AsNoTracking()
                .Include(p => p.PurchaseRequestItems)
                .ThenInclude(item => item.Quotations)
                .FirstAsync(p => p.Id == purchaseRequestId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedPurchaseRequest.RequestStatus, Is.EqualTo(PurchaseRequestStatus.Rejected));
                Assert.That(updatedPurchaseRequest.AnnulmentReason, Is.EqualTo(payload.Reason));
                Assert.That(updatedPurchaseRequest.AnnulledByUserId, Is.EqualTo(userId));
                Assert.That(updatedPurchaseRequest.DeletedAt, Is.Not.Null);
            });

            // Assert - Validar que los ítems ya no tengan cotización activa y las cotizaciones estén inactivas con soft delete
            Assert.That(updatedPurchaseRequest.PurchaseRequestItems, Is.Not.Empty);
            foreach (var item in updatedPurchaseRequest.PurchaseRequestItems)
            {
                Assert.That(item.HasQuotation, Is.False);
                Assert.That(item.Quotations, Is.Not.Empty);
                foreach (var quote in item.Quotations)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(quote.IsActive, Is.False);
                        Assert.That(quote.IsAcceptedForPurchase, Is.False);
                        Assert.That(quote.DeletedAt, Is.Not.Null);
                    });
                }
            }
        }

        /// <summary>
        /// Verifica la regla de negocio que prohíbe anular una solicitud de compra si ya cuenta
        /// con una orden de compra generada, retornando BadRequest.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task AnnulPurchaseRequest_WhenPurchaseOrderExists_ShouldReturnBadRequest(string companyAlias)
        {
            // Arrange - Generar solicitud y su orden de compra emitida asociada
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);
            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Approved);

            await CreatePurchaseOrder(purchaseRequestId, userId);

            var payload = new AnnulPurchaseRequestCommand
            {
                Reason = "Intento de anulación sobre solicitud con orden de compra ya emitida"
            };

            // Act - Intentar anulación
            var response = await SendRequestAsync(HttpMethod.Post, AnnulUrl(companyId, purchaseRequestId), token, payload);

            // Assert - Debe responder 400 BadRequest
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        #endregion

        #region Reactivación / Reenvío a Revisión Contable

        /// <summary>
        /// Verifica que tras una devolución contable con alcance QuotationOnly, al agregar una nueva cotización
        /// y reenviar la solicitud a revisión, la entidad existente de revisión contable se reactive
        /// sin duplicar registros en la base de datos.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task ResendPurchaseRequest_AfterAccountingQuotationOnlyReturn_ShouldReactivateReviewWithoutDuplicates(string companyAlias)
        {
            // Arrange - 1. Crear solicitud con revisión contable pendiente
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);

            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Revision);
            var initialReviewId = await CreateAccountingReview(purchaseRequestId, userId, AccountingReviewStatus.Pending);

            // Arrange - 2. Contabilidad retorna la revisión con alcance QuotationOnly
            var annulAccountingPayload = new AnnulAccountingReviewCommand
            {
                Scope = AnnulmentScope.QuotationOnly,
                Reason = "Las cotizaciones no cumplen especificaciones técnicas."
            };
            var annulResponse = await SendRequestAsync(
                HttpMethod.Post,
                AnnulAccountingUrl(companyId, initialReviewId),
                token,
                annulAccountingPayload);
            Assert.That(annulResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Arrange - 3. Se adjunta nueva cotización activa y válida
            await AddQuotation(purchaseRequestId, isAccepted: true);

            // Arrange - 4. Preparar comando para re-enviar a revisión contable
            var resendPayload = new SendPurchaseRequestToReviewCommand
            {
                Comments = "Se adjunta nueva cotización corregida según observaciones."
            };

            // Act - Reenviar a revisión contable
            var resendResponse = await SendRequestAsync(HttpMethod.Post, SendAccountingReviewUrl(companyId, purchaseRequestId), token, resendPayload);

            // Assert - Debe responder 204 NoContent
            Assert.That(resendResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            // Assert - Verificar que NO se duplicó el registro de revisión contable en BD
            var allReviews = await _unitOfWork.PurchaseRequestsReviewedAccounting.Entities
                .AsNoTracking()
                .Where(r => r.PurchaseRequestId == purchaseRequestId)
                .ToListAsync();

            Assert.That(allReviews, Has.Count.EqualTo(1));

            // Assert - Verificar que la entidad original fue reactivada en estado Pending
            var reactivatedReview = allReviews.First();
            Assert.Multiple(() =>
            {
                Assert.That(reactivatedReview.Id, Is.EqualTo(initialReviewId));
                Assert.That(reactivatedReview.Status, Is.EqualTo(AccountingReviewStatus.Pending));
                Assert.That(reactivatedReview.DeletedAt, Is.Null);
                Assert.That(reactivatedReview.ReviewedByUserId, Is.Null);
                Assert.That(reactivatedReview.Comments, Is.EqualTo(resendPayload.Comments));
                Assert.That(reactivatedReview.SentByUserId, Is.EqualTo(userId));
            });

            // Assert - Solicitud de compra volvió a estado Revision
            var updatedPurchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .AsNoTracking()
                .FirstAsync(p => p.Id == purchaseRequestId);

            Assert.That(updatedPurchaseRequest.RequestStatus, Is.EqualTo(PurchaseRequestStatus.Revision));
        }

        /// <summary>
        /// Verifica la regla de validación que impide reenviar una solicitud a revisión contable
        /// si los ítems no cuentan con cotizaciones activas, respondiendo 400 BadRequest.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task ResendPurchaseRequest_WhenItemsHaveNoQuotation_ShouldReturnBadRequest(string companyAlias)
        {
            // Arrange - 1. Crear solicitud y revisión contable inicial
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);

            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Revision);
            var reviewId = await CreateAccountingReview(purchaseRequestId, userId, AccountingReviewStatus.Pending);

            // Arrange - 2. Retornar por cotizaciones (anula las cotizaciones existentes)
            var annulPayload = new AnnulAccountingReviewCommand
            {
                Scope = AnnulmentScope.QuotationOnly,
                Reason = "Se requieren nuevas cotizaciones."
            };
            await SendRequestAsync(HttpMethod.Post, AnnulAccountingUrl(companyId, reviewId), token, annulPayload);

            // Arrange - 3. Intentar re-enviar SIN agregar nuevas cotizaciones activas
            var resendPayload = new SendPurchaseRequestToReviewCommand
            {
                Comments = "Reenvío sin cotizaciones."
            };

            // Act - Reenviar a revisión
            var resendResponse = await SendRequestAsync(HttpMethod.Post, SendAccountingReviewUrl(companyId, purchaseRequestId), token, resendPayload);

            // Assert - Debe responder 400 BadRequest
            Assert.That(resendResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        #endregion
    }
}
