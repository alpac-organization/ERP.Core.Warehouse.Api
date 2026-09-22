# Revisiones de gerencia

## Anular / Retornar Revisión de Gerencia

Endpoint para anular o retornar una revisión de solicitud de compra desde la gerencia dentro del módulo de purchase.

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/requisition-management-reviews/{requisition_management_reviews_id}/annul` |
| **Descripción** | Permite a la gerencia anular o devolver una revisión en estado `Pending`. Admite dos alcances: `QuotationOnly` (1), que devuelve la solicitud al estado `Approved` y la revisión contable previa a `Returned` para permitir un nuevo ciclo de cotización; o `FullProcess` (2), que anula definitivamente todo el trámite de compra tanto en gerencia como en contabilidad y en la solicitud. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `requisition_management_reviews_id` | `guid` | Sí | Identificador único de la revisión de gerencia. Debe existir y estar en estado `Pending`. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id`, `module_code` y `requisition_management_reviews_id` se toman del token JWT y de la ruta (`[JsonIgnore]` / route).

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `scope` | `integer (enum AnnulmentScope)` | Sí | Alcance de la anulación. Valores permitidos: <br>• `1` (`QuotationOnly`): Anula las cotizaciones y devuelve la solicitud a compras con efecto cascada en la revisión contable.<br>• `2` (`FullProcess`): Anula definitivamente todo el trámite de compra en todas las etapas. |
| `reason` | `string` | Sí | Motivo o justificación de la anulación o retorno. Máximo 1000 caracteres. Se almacena en `AnnulmentReason`. |

### Ejemplo: Retorno a cotización (`QuotationOnly`)
```json
{
  "scope": 1,
  "reason": "Precios no competitivos según análisis gerencial, solicitar nuevas cotizaciones."
}
```

### Ejemplo: Anulación definitiva del trámite (`FullProcess`)
```json
{
  "scope": 2,
  "reason": "Cancelación definitiva del requerimiento por disposición de la dirección."
}
```

---

## Respuestas

### ✅ 200 OK

La revisión de gerencia fue procesada correctamente según el alcance seleccionado. Retorna status 200 con cuerpo vacío.

---

### ❌ 400 Bad Request

Se retorna cuando no se cumplen las reglas de negocio o fallan las validaciones de entrada (`ErrorResponse`):

```json
{
  "status": 400,
  "error": {
    "type_error": "BadRequest",
    "description": "La solicitud ya cuenta con una orden de compra generada, no se puede anular ni retornar"
  },
  "created_at": "2026-09-22 10:00:00"
}
```

### ⛔ 403 Forbidden

```json
{
  "status": 403,
  "error": {
    "type_error": "Forbidden",
    "description": "No tienes permiso para anular o retornar revisiones de gerencia"
  },
  "created_at": "2026-09-22 10:00:00"
}
```

### 🔍 404 Not Found

```json
{
  "status": 404,
  "error": {
    "type_error": "NotFound",
    "description": "La revisión de gerencia no fue encontrada o ya fue procesada"
  },
  "created_at": "2026-09-22 10:00:00"
}
```

### 💥 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud"
  },
  "created_at": "2026-09-22 10:00:00"
}
```

---

## Reglas de Negocio

| Regla | Comportamiento | Código de Error |
|---|---|---|
| **Roles no autorizados** | Los roles `Supervisor` y `Operator` no tienen permiso para anular ni retornar revisiones de gerencia. | `ERP:FORBIDDEN` (403) |
| **Estado de la revisión** | La revisión de gerencia debe estar en estado `Pending`. | `ERP:INVALID_STATUS` (400) |
| **Orden de compra ya generada** | No se puede anular ni retornar si la solicitud asociada ya cuenta con una orden de compra generada (`PurchaseOrder != null`). | `ERP:PURCHASE_ORDER_ALREADY_EXISTS` (400) |
| **Alcance válido (`scope`)** | Debe ser `1` (`QuotationOnly`) o `2` (`FullProcess`). | `ERP:INVALID_SCOPE` (400) |
| **Revisión no encontrada** | Si no existe o tiene `DeletedAt != null`. | `ERP:MANAGEMENT_REVIEW_NOT_FOUND` (404) |
| **Solicitud no encontrada** | Si la solicitud vinculada no existe en el sistema. | `ERP:PURCHASE_REQUEST_NOT_FOUND` (404) |
| **Motivo obligatorio** | `reason` no puede estar vacío ni superar los 1000 caracteres. | Validación FluentValidation (400) |

---

## Comportamiento según el Alcance (`scope`)

| Componente afectado | Alcance `QuotationOnly` (1) | Alcance `FullProcess` (2) |
|---|---|---|
| **Revisión de Gerencia (`PurchaseRequestsReviewedManagement`)** | • `Status`: `Rejected`<br>• `ReviewedByUserId`: Usuario autenticado<br>• `DeletedAt`: `DateTime.UtcNow` | • `Status`: `Rejected`<br>• `ReviewedByUserId`: Usuario autenticado<br>• `DeletedAt`: `DateTime.UtcNow` |
| **Revisión Contable Previa (`PurchaseRequestsReviewedAccounting`)** *(Efecto Cascada)* | • `Status`: `Returned`<br>• `DeletedAt`: `DateTime.UtcNow` | • `Status`: `Rejected`<br>• `DeletedAt`: `DateTime.UtcNow` |
| **Solicitud de Compra (`PurchaseRequests`)** | • `RequestStatus`: `Approved`<br>• `AnnulmentReason`: Motivo enviado<br>• `AnnulledByUserId`: Usuario autenticado<br>• `DeletedAt`: `null` *(continúa viva en el sistema)* | • `RequestStatus`: `Rejected`<br>• `AnnulmentReason`: Motivo enviado<br>• `AnnulledByUserId`: Usuario autenticado<br>• `DeletedAt`: `DateTime.UtcNow` *(baja definitiva)* |
| **Ítems (`PurchaseRequestItems`)** | • `HasQuotation`: `false` | • `HasQuotation`: `false` |
| **Cotizaciones (`Quotations`)** | • `IsActive`: `false`<br>• `IsAcceptedForPurchase`: `false`<br>• `DeletedAt`: `DateTime.UtcNow` | • `IsActive`: `false`<br>• `IsAcceptedForPurchase`: `false`<br>• `DeletedAt`: `DateTime.UtcNow` |

---

## Códigos de Estado

| Código | Descripción |
|:---:|---|
| `200` | Revisión de gerencia procesada exitosamente. |
| `400` | Error de validación o incumplimiento de reglas de negocio (`ErrorResponse`). |
| `403` | Permisos insuficientes para el rol del usuario autenticado (`ErrorResponse`). |
| `404` | La revisión de gerencia o la solicitud asociada no fue encontrada (`ErrorResponse`). |
| `500` | Error interno no controlado en el servidor (`ErrorResponse`). |
