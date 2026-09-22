# Solicitudes de compras

## Anular Solicitud de Compra

Endpoint para anular de forma definitiva una solicitud de compra dentro del módulo de purchase.

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}/annul` |
| **Descripción** | Anula definitivamente una solicitud de compra activa. Registra el motivo de anulación (`AnnulmentReason`) y el usuario que ejecuta la acción (`AnnulledByUserId`). La solicitud pasa al estado `Rejected` con borrado lógico (`DeletedAt`). Da de baja las revisiones activas (contable o de gerencia) y desmarca / invalida todas las cotizaciones vinculadas a sus ítems. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `purchase_request_id` | `guid` | Sí | Identificador único de la solicitud de compra a anular. Debe existir y estar activa. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id`, `module_code` y `purchase_request_id` no forman parte del JSON del cuerpo: se obtienen directamente del token JWT y de la ruta (`[JsonIgnore]` / route).

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `reason` | `string` | Sí | Motivo o justificación de la anulación. Máximo 1000 caracteres. Se almacena en `AnnulmentReason`. |

```json
{
  "reason": "Cancelación presupuestaria por cambio de prioridades operativas."
}
```

---

## Respuestas

### ✅ 200 OK

La solicitud de compra fue anulada correctamente. Retorna status 200 con cuerpo vacío.

---

### ❌ 400 Bad Request

Se retorna cuando la solicitud no cumple las reglas de negocio o falla la validación de entrada (`ErrorResponse`):

```json
{
  "status": 400,
  "error": {
    "type_error": "BadRequest",
    "description": "La solicitud de compra ya se encuentra en un estado terminal (rechazada, cancelada o finalizada)"
  },
  "created_at": "2026-09-22 10:00:00"
}
```

### ⛔ 403 Forbidden

Se retorna cuando el rol del usuario no tiene permisos para anular la solicitud:

```json
{
  "status": 403,
  "error": {
    "type_error": "Forbidden",
    "description": "No tienes permiso para anular la solicitud de compra"
  },
  "created_at": "2026-09-22 10:00:00"
}
```

### 🔍 404 Not Found

Se retorna cuando la solicitud de compra no existe o ya ha sido eliminada lógicamente:

```json
{
  "status": 404,
  "error": {
    "type_error": "NotFound",
    "description": "La solicitud de compra no fue encontrada o ya fue anulada"
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
| **Rol `Supervisor`** | No tiene permiso para anular solicitudes de compra. | `ERP:FORBIDDEN` (403) |
| **Rol `Operator` (propietario)** | Un operador solo puede anular las solicitudes creadas por él mismo (`RegisteredByUserId == UserId`). | `ERP:FORBIDDEN` (403) |
| **Rol `Operator` en revisión** | El operador no puede anular si la solicitud ya está en proceso de revisión por Finanzas o Gerencia (`RequestStatus == Revision` o revisiones activas). | `ERP:PURCHASE_REQUEST_IN_REVIEW` (400) |
| **Estados terminales** | No se puede anular si ya está en estado `Rejected`, `Canceled` o `Finished`. | `ERP:PURCHASE_REQUEST_ALREADY_TERMINATED` (400) |
| **Aprobación previa o con orden** | No se puede anular si ya fue aprobada por gerencia (`ManagementReview.Status == Approved`) o cuenta con una orden de compra emitida (`PurchaseOrder != null`). | `ERP:PURCHASE_REQUEST_ALREADY_APPROVED` (400) |
| **Inexistente o inactiva** | Si no existe o tiene `DeletedAt != null`. | `ERP:PURCHASE_REQUEST_NOT_FOUND` (404) |
| **Motivo obligatorio** | `reason` no puede estar vacío ni superar los 1000 caracteres. | Validación FluentValidation (400) |

---

## Efectos en Base de Datos

Al completarse exitosamente la anulación:

1. **`PurchaseRequests`:**
   - `RequestStatus`: Pasa a `PurchaseRequestStatus.Rejected`.
   - `AnnulmentReason`: Se almacena el texto enviado en `reason`.
   - `AnnulledByUserId`: Se registra el identificador del usuario que ejecutó la anulación.
   - `DeletedAt`: Se marca con la fecha y hora UTC actual (`DateTime.UtcNow`).
2. **`PurchaseRequestsReviewedAccounting` (si existía activa):**
   - `Status`: Pasa a `AccountingReviewStatus.Rejected`.
   - `DeletedAt`: Marcado con la fecha UTC actual.
3. **`PurchaseRequestsReviewedManagement` (si existía activa):**
   - `Status`: Pasa a `ManagementReviewStatus.Rejected`.
   - `DeletedAt`: Marcado con la fecha UTC actual.
4. **`PurchaseRequestItems`:**
   - `HasQuotation`: Se desmarca a `false`.
5. **`Quotations` vinculadas a los ítems:**
   - `IsActive`: Pasa a `false`.
   - `IsAcceptedForPurchase`: Pasa a `false`.
   - `DeletedAt`: Marcado con la fecha UTC actual.

---

## Códigos de Estado

| Código | Descripción |
|:---:|---|
| `200` | Solicitud de compra anulada exitosamente. |
| `400` | Error de validación en el cuerpo o incumplimiento de reglas de negocio (`ErrorResponse`). |
| `403` | Permisos insuficientes para el rol del usuario autenticado (`ErrorResponse`). |
| `404` | La solicitud de compra no existe o ya está eliminada (`ErrorResponse`). |
| `500` | Error interno no controlado en el servidor (`ErrorResponse`). |
