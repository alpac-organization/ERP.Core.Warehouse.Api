# Revisiones contables

## Anular / Retornar Revisión Contable

Endpoint para anular o retornar una revisión de solicitud de compra desde el área de contabilidad / finanzas dentro del módulo de purchase.

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/requisition-accounting-reviews/{requisition_accounting_review_id}/annul` |
| **Descripción** | Permite al área contable anular o devolver una revisión en estado `Pending`. Admite dos alcances: `QuotationOnly` (1), que devuelve la solicitud al estado `Approved` para permitir que el departamento de compras gestione nuevas cotizaciones; o `FullProcess` (2), que anula definitivamente todo el trámite de la solicitud de compra. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `requisition_accounting_review_id` | `guid` | Sí | Identificador único de la revisión contable. Debe existir y estar en estado `Pending`. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id`, `module_code` y `requisition_accounting_review_id` se toman del token JWT y de la ruta (`[JsonIgnore]` / route).

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `scope` | `integer (enum AnnulmentScope)` | Sí | Alcance de la anulación. Valores permitidos: <br>• `1` (`QuotationOnly`): Anula únicamente las cotizaciones y retorna la solicitud a compras.<br>• `2` (`FullProcess`): Anula definitivamente todo el trámite de compra. |
| `reason` | `string` | Sí | Motivo o justificación de la anulación o retorno. Máximo 1000 caracteres. Se almacena en `AnnulmentReason`. |

### Ejemplo: Retorno a cotización (`QuotationOnly`)
```json
{
  "scope": 1,
  "reason": "Las cotizaciones superan el presupuesto asignado para el área."
}
```

### Ejemplo: Anulación definitiva del trámite (`FullProcess`)
```json
{
  "scope": 2,
  "reason": "Cancelación del proyecto por reestructuración presupuestaria."
}
```

---

## Respuestas

### ✅ 200 OK

La revisión contable fue procesada correctamente según el alcance seleccionado. Retorna status 200 con cuerpo vacío.

---

### ❌ 400 Bad Request

Se retorna cuando no se cumplen las reglas de negocio o fallan las validaciones de entrada (`ErrorResponse`):

```json
{
  "status": 400,
  "error": {
    "type_error": "BadRequest",
    "description": "La revisión contable ya no se encuentra en estado pendiente"
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
    "description": "No tienes permiso para realizar esta acción"
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
    "description": "La revisión contable no fue encontrada o ya fue procesada"
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
| **Rol `Supervisor`** | No tiene permiso para anular o retornar revisiones contables. | `ERP:FORBIDDEN` (403) |
| **Estado de la revisión** | La revisión contable debe estar en estado `Pending`. | `ERP:INVALID_STATUS` (400) |
| **Aprobación de gerencia u orden existente** | No se puede anular ni retornar si la solicitud ya fue aprobada por gerencia (`ManagementReview.Status == Approved`) o cuenta con una orden de compra generada (`PurchaseOrder != null`). | `ERP:PURCHASE_REQUEST_ALREADY_APPROVED` (400) |
| **Alcance válido (`scope`)** | Debe ser `1` (`QuotationOnly`) o `2` (`FullProcess`). | `ERP:INVALID_SCOPE` (400) |
| **Revisión no encontrada** | Si no existe o tiene `DeletedAt != null`. | `ERP:ACCOUNTING_REVIEW_NOT_FOUND` (404) |
| **Solicitud no encontrada** | Si la solicitud vinculada no existe en el sistema. | `ERP:PURCHASE_REQUEST_NOT_FOUND` (404) |
| **Motivo obligatorio** | `reason` no puede estar vacío ni superar los 1000 caracteres. | Validación FluentValidation (400) |

---

## Comportamiento según el Alcance (`scope`)

| Componente afectado | Alcance `QuotationOnly` (1) | Alcance `FullProcess` (2) |
|---|---|---|
| **Revisión Contable (`PurchaseRequestsReviewedAccounting`)** | • `Status`: `Returned`<br>• `ReviewedByUserId`: Usuario autenticado<br>• `DeletedAt`: `DateTime.UtcNow` | • `Status`: `Rejected`<br>• `ReviewedByUserId`: Usuario autenticado<br>• `DeletedAt`: `DateTime.UtcNow` |
| **Solicitud de Compra (`PurchaseRequests`)** | • `RequestStatus`: `Approved`<br>• `AnnulmentReason`: Motivo enviado<br>• `AnnulledByUserId`: Usuario autenticado<br>• `DeletedAt`: `null` *(continúa viva en el sistema)* | • `RequestStatus`: `Rejected`<br>• `AnnulmentReason`: Motivo enviado<br>• `AnnulledByUserId`: Usuario autenticado<br>• `DeletedAt`: `DateTime.UtcNow` *(baja definitiva)* |
| **Ítems (`PurchaseRequestItems`)** | • `HasQuotation`: `false` | • `HasQuotation`: `false` |
| **Cotizaciones (`Quotations`)** | • `IsActive`: `false`<br>• `IsAcceptedForPurchase`: `false`<br>• `DeletedAt`: `DateTime.UtcNow` | • `IsActive`: `false`<br>• `IsAcceptedForPurchase`: `false`<br>• `DeletedAt`: `DateTime.UtcNow` |

---

## Códigos de Estado

| Código | Descripción |
|:---:|---|
| `200` | Revisión contable procesada exitosamente. |
| `400` | Error de validación o incumplimiento de reglas de negocio (`ErrorResponse`). |
| `403` | Permisos insuficientes para el rol del usuario autenticado (`ErrorResponse`). |
| `404` | La revisión contable o la solicitud asociada no fue encontrada (`ErrorResponse`). |
| `500` | Error interno no controlado en el servidor (`ErrorResponse`). |
