using System.Net;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionAccountingReviews.v1.Commands;

namespace ERP.Core.Warehouse.Api.Test.Controllers.purchasing
{
    [TestFixture]
    public class RequisitionAccountingReviewControllerTest : IntegrationTestUtilsBase
    {
        private const string ModuleCode = "COM-129U";

        private static string AnnulUrl(Guid companyId, Guid reviewId) =>
            $"/api/v1/companies/{companyId}/modules/{ModuleCode}/requisition-accounting-reviews/{reviewId}/annul";

        /// <summary>
        /// Verifica que una revisión contable en estado Pending pueda anularse con alcance QuotationOnly,
        /// pasando la revisión contable a estado Returned (con soft delete), y retornando la solicitud de compra
        /// al estado Approved (sin soft delete) para permitir nueva cotización, invalidando las cotizaciones previas.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task AnnulAccountingReview_WithQuotationOnly_ShouldReturnToApprovedAndInvalidateQuotes(string companyAlias)
        {
            // Arrange - Generar usuario operador, solicitud en Revision y revisión contable Pending
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);

            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Revision);
            var reviewId = await CreateAccountingReview(purchaseRequestId, userId, AccountingReviewStatus.Pending);

            var payload = new AnnulAccountingReviewCommand
            {
                Scope = AnnulmentScope.QuotationOnly,
                Reason = "Las cotizaciones superan el presupuesto asignado para el área."
            };

            // Act - Enviar anulación parcial (retorno de cotizaciones)
            var response = await SendRequestAsync(HttpMethod.Post, AnnulUrl(companyId, reviewId), token, payload);

            // Assert - HTTP Status 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Assert - Revisión Contable en estado Returned con soft delete
            var updatedReview = await _unitOfWork.PurchaseRequestsReviewedAccounting.Entities
                .AsNoTracking()
                .FirstAsync(r => r.Id == reviewId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedReview.Status, Is.EqualTo(AccountingReviewStatus.Returned));
                Assert.That(updatedReview.DeletedAt, Is.Not.Null);
                Assert.That(updatedReview.ReviewedByUserId, Is.EqualTo(userId));
            });

            // Assert - Solicitud de Compra debe permanecer viva en Approved para re-cotizar
            var updatedPurchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .AsNoTracking()
                .Include(p => p.PurchaseRequestItems)
                    .ThenInclude(i => i.Quotations)
                .FirstAsync(p => p.Id == purchaseRequestId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedPurchaseRequest.RequestStatus, Is.EqualTo(PurchaseRequestStatus.Approved));
                Assert.That(updatedPurchaseRequest.AnnulmentReason, Is.EqualTo(payload.Reason));
                Assert.That(updatedPurchaseRequest.AnnulledByUserId, Is.EqualTo(userId));
                Assert.That(updatedPurchaseRequest.DeletedAt, Is.Null);
            });

            // Assert - Cotizaciones dadas de baja e ítem sin cotización activa
            foreach (var item in updatedPurchaseRequest.PurchaseRequestItems)
            {
                Assert.That(item.HasQuotation, Is.False);
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
        /// Verifica que una revisión contable en estado Pending pueda anularse con alcance FullProcess,
        /// rechazando tanto la revisión contable como la solicitud de compra de forma definitiva (soft delete total)
        /// y cancelando todas las cotizaciones.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task AnnulAccountingReview_WithFullProcess_ShouldRejectEntireProcess(string companyAlias)
        {
            // Arrange - Generar usuario operador, solicitud y revisión contable inicial
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);

            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Revision);
            var reviewId = await CreateAccountingReview(purchaseRequestId, userId, AccountingReviewStatus.Pending);

            var payload = new AnnulAccountingReviewCommand
            {
                Scope = AnnulmentScope.FullProcess,
                Reason = "Cancelación definitiva del trámite por cierre de proyecto."
            };

            // Act - Enviar anulación total
            var response = await SendRequestAsync(HttpMethod.Post, AnnulUrl(companyId, reviewId), token, payload);

            // Assert - HTTP Status 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Assert - Revisión Contable en estado Rejected con soft delete
            var updatedReview = await _unitOfWork.PurchaseRequestsReviewedAccounting.Entities
                .AsNoTracking()
                .FirstAsync(r => r.Id == reviewId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedReview.Status, Is.EqualTo(AccountingReviewStatus.Rejected));
                Assert.That(updatedReview.DeletedAt, Is.Not.Null);
            });

            // Assert - Solicitud de Compra anulada definitivamente con soft delete
            var updatedPurchaseRequest = await _unitOfWork.PurchaseRequests.Entities
                .AsNoTracking()
                .Include(p => p.PurchaseRequestItems)
                    .ThenInclude(i => i.Quotations)
                .FirstAsync(p => p.Id == purchaseRequestId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedPurchaseRequest.RequestStatus, Is.EqualTo(PurchaseRequestStatus.Rejected));
                Assert.That(updatedPurchaseRequest.AnnulmentReason, Is.EqualTo(payload.Reason));
                Assert.That(updatedPurchaseRequest.AnnulledByUserId, Is.EqualTo(userId));
                Assert.That(updatedPurchaseRequest.DeletedAt, Is.Not.Null);
            });

            // Assert - Cotizaciones dadas de baja
            foreach (var item in updatedPurchaseRequest.PurchaseRequestItems)
            {
                Assert.That(item.HasQuotation, Is.False);
                foreach (var quote in item.Quotations)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(quote.IsActive, Is.False);
                        Assert.That(quote.DeletedAt, Is.Not.Null);
                    });
                }
            }
        }
    }
}
