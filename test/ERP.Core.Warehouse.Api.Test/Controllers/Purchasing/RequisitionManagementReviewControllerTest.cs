using System.Net;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Application.Features.RequisitionManagementReviews.v1.Commands;

namespace ERP.Core.Warehouse.Api.Test.Controllers.purchasing
{
    [TestFixture]
    public class RequisitionManagementReviewControllerTest : IntegrationTestUtilsBase
    {
        private const string ModuleCode = "COM-129U";

        private static string AnnulUrl(Guid companyId, Guid reviewId) =>
            $"/api/v1/companies/{companyId}/modules/{ModuleCode}/requisition-management-reviews/{reviewId}/annul";

        /// <summary>
        /// Verifica que una revisión de gerencia en estado Pending pueda anularse con alcance QuotationOnly (por un rol autorizado),
        /// pasando la revisión de gerencia a estado Rejected (con soft delete), aplicando efecto cascada a la revisión contable previa
        /// (pasando a Returned con soft delete), y reactivando la solicitud de compra al estado Approved (sin soft delete) para permitir
        /// nueva cotización, invalidando todas las cotizaciones previas.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task AnnulManagementReview_WithQuotationOnly_ShouldCascadeToAccountingAndReactivateRequest(string companyAlias)
        {
            // Arrange - Generar usuario Administrador, solicitud en Revision, revisión contable Approved y revisión de gerencia Pending
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Administrator, companyAlias);

            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Revision);
            var accountingReviewId = await CreateAccountingReview(purchaseRequestId, userId, AccountingReviewStatus.Approved);
            var managementReviewId = await CreateManagementReview(purchaseRequestId, userId, ManagementReviewStatus.Pending);

            var payload = new AnnulManagementReviewCommand
            {
                Scope = AnnulmentScope.QuotationOnly,
                Reason = "Precios no competitivos según análisis gerencial, solicitar nuevas cotizaciones."
            };

            // Act - Enviar anulación de gerencia con alcance QuotationOnly
            var response = await SendRequestAsync(HttpMethod.Post, AnnulUrl(companyId, managementReviewId), token, payload);

            // Assert - HTTP Status 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Assert - Revisión de Gerencia pasa a Rejected con soft delete
            var updatedManagementReview = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .AsNoTracking()
                .FirstAsync(r => r.Id == managementReviewId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedManagementReview.Status, Is.EqualTo(ManagementReviewStatus.Rejected));
                Assert.That(updatedManagementReview.DeletedAt, Is.Not.Null);
                Assert.That(updatedManagementReview.ReviewedByUserId, Is.EqualTo(userId));
            });

            // Assert - Cascada a Revisión Contable previa (debe pasar a Returned con soft delete)
            var updatedAccountingReview = await _unitOfWork.PurchaseRequestsReviewedAccounting.Entities
                .AsNoTracking()
                .FirstAsync(r => r.Id == accountingReviewId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedAccountingReview.Status, Is.EqualTo(AccountingReviewStatus.Returned));
                Assert.That(updatedAccountingReview.DeletedAt, Is.Not.Null);
            });

            // Assert - Solicitud de Compra (debe volver a Approved para re-cotizar sin soft delete)
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

            // Assert - Items y Cotizaciones invalidadas
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
        /// Verifica que una revisión de gerencia en estado Pending pueda anularse con alcance FullProcess (por un rol autorizado),
        /// rechazando de forma definitiva todas las etapas del proceso (revisión de gerencia, revisión contable previa y solicitud de compra)
        /// aplicando soft delete total e invalidando todas las cotizaciones asociadas.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task AnnulManagementReview_WithFullProcess_ShouldRejectAllStages(string companyAlias)
        {
            // Arrange - Generar usuario Administrador, solicitud, revisión contable previa y revisión de gerencia inicial
            var (companyId, userId, token) = await ArrangeUserWithRole(RoleType.Administrator, companyAlias);

            var purchaseRequestId = await CreatePurchaseRequest(userId, PurchaseRequestStatus.Revision);
            var accountingReviewId = await CreateAccountingReview(purchaseRequestId, userId, AccountingReviewStatus.Approved);
            var managementReviewId = await CreateManagementReview(purchaseRequestId, userId, ManagementReviewStatus.Pending);

            var payload = new AnnulManagementReviewCommand
            {
                Scope = AnnulmentScope.FullProcess,
                Reason = "Cancelación definitiva por recorte de presupuesto institucional."
            };

            // Act - Enviar anulación definitiva del proceso completo (FullProcess)
            var response = await SendRequestAsync(HttpMethod.Post, AnnulUrl(companyId, managementReviewId), token, payload);

            // Assert - HTTP Status 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Assert - Revisión de Gerencia rechazada con soft delete
            var updatedManagementReview = await _unitOfWork.PurchaseRequestsReviewedManagement.Entities
                .AsNoTracking()
                .FirstAsync(r => r.Id == managementReviewId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedManagementReview.Status, Is.EqualTo(ManagementReviewStatus.Rejected));
                Assert.That(updatedManagementReview.DeletedAt, Is.Not.Null);
            });

            // Assert - Revisión Contable previa rechazada con soft delete
            var updatedAccountingReview = await _unitOfWork.PurchaseRequestsReviewedAccounting.Entities
                .AsNoTracking()
                .FirstAsync(r => r.Id == accountingReviewId);

            Assert.Multiple(() =>
            {
                Assert.That(updatedAccountingReview.Status, Is.EqualTo(AccountingReviewStatus.Rejected));
                Assert.That(updatedAccountingReview.DeletedAt, Is.Not.Null);
            });

            // Assert - Solicitud de Compra rechazada definitivamente con soft delete
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

        /// <summary>
        /// Verifica la restricción de seguridad por rol: un usuario con rol Operator (u otro rol sin permisos directivos)
        /// no tiene autorización para anular una revisión de gerencia, retornando un código de estado HTTP 403 Forbidden.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllCompanies))]
        public async Task AnnulManagementReview_WhenUserIsOperator_ShouldReturnForbidden(string companyAlias)
        {
            // Arrange - Generar usuario con rol Operator y solicitud con revisión de gerencia inicial
            var (companyId, operatorUserId, token) = await ArrangeUserWithRole(RoleType.Operator, companyAlias);

            var purchaseRequestId = await CreatePurchaseRequest(operatorUserId, PurchaseRequestStatus.Revision);
            var managementReviewId = await CreateManagementReview(purchaseRequestId, operatorUserId, ManagementReviewStatus.Pending);

            var payload = new AnnulManagementReviewCommand
            {
                Scope = AnnulmentScope.QuotationOnly,
                Reason = "Operador intentando anular una revisión de gerencia"
            };

            // Act - Intentar anular la revisión de gerencia con token de Operador
            var response = await SendRequestAsync(HttpMethod.Post, AnnulUrl(companyId, managementReviewId), token, payload);

            // Assert - Debe responder con HTTP 403 Forbidden
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }
}
