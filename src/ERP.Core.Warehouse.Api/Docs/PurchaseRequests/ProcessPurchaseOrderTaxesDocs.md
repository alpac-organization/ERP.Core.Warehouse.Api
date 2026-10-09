# Órdenes de compra

## Procesar Revisión de Gerencia y Emitir Orden(es) de Compra

Endpoint que aprueba o rechaza la revisión de gerencia. Si se aprueba, genera **una orden de compra por proveedor** a partir de las cotizaciones aceptadas, calcula retenciones IMI/IR cuando corresponde y persiste la metadata fiscal.

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/purchase-orders/{requisition_management_review_id}/process` |
| **Descripción** | Procesa la revisión de gerencia. En `Approved`: crea OC(s) con ítems, código único, supplier y cálculos de IMI/IR. En `Rejected`: solo actualiza el estado de la revisión. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo. |
| `requisition_management_review_id` | `guid` | Sí | Revisión de gerencia en estado `Pending`. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `new_status` | `integer` | Sí | `ManagementReviewStatus`. No puede ser `Pending`. Usar Approved o Rejected. |
| `comments` | `string` | Condicional | Comentarios de la revisión. Obligatorio según validator cuando se aprueba/rechaza. |

### Ejemplo — Aprobar

```json
{
  "new_status": 2,
  "comments": "Aprobado por gerencia"
}
```

> El valor numérico exacto de `Approved` / `Rejected` depende del enum `ManagementReviewStatus` del Core. Consultar Swagger o el catálogo de enums del proyecto.

### Ejemplo — Rechazar

```json
{
  "new_status": 3,
  "comments": "Presupuesto insuficiente"
}
```

---

## Qué genera el backend al aprobar

1. Agrupa cotizaciones con `is_accepted_for_purchase = true` por `supplier_id`.
2. Por cada supplier:
   - Crea `PurchaseOrder` (código, `supplier_id`, comentarios).
   - Crea `PurchaseOrderItem` por cada cotización aceptada (producto, cantidad, `unit_price`).
   - Calcula impuestos/retenciones y los anexa a `comments`.

Si no hay cotizaciones aceptadas → `400` `ERP:NO_ACCEPTED_QUOTATIONS`.

---

## Cálculo de impuestos y retenciones

### Totales

```text
subtotal   = Σ price_total de cotizaciones aceptadas del supplier
iva_total  = Σ iva de esas cotizaciones
total_nio  = subtotal + iva_total
```

### Tipo de cambio

- Se obtiene de `ValidityDeductions` con `TaxType.ExchangeRate` (valor vigente).
- Fallback: `36.6243` si no hay registro activo.

### IMI / IR

Se aplican **solo si**:

1. `total_nio >= 1000` (córdobas), **y**
2. `SupplierDetails.IsTaxExempt == false`

| Impuesto | TaxType | Origen |
|---|---|---|
| IMI | `7` (`Imi`) | `ValidityDeductions.Value` (% ) |
| IR | `8` (`Ir`) | `ValidityDeductions.Value` (% ) |

```text
imi_amount = total_nio × (imi_rate / 100)
ir_amount  = total_nio × (ir_rate / 100)
```

Si no aplican retenciones, ambos montos quedan en `0`.

---

## Metadata fiscal en `comments`

El backend concatena a los comentarios del usuario un bloque JSON:

```text
Aprobado por gerencia
---TAX---{"exchange_rate":36.6243,"total_nio":1135.00,"imi_amount":11.35,"ir_amount":22.70,"imi_rate":1.0,"ir_rate":2.0,"retentions_applied":true}
```

| Campo | Descripción |
|---|---|
| `exchange_rate` | Tipo de cambio usado. |
| `total_nio` | Total en córdobas (subtotal + IVA). |
| `imi_amount` | Monto IMI retenido. |
| `ir_amount` | Monto IR retenido. |
| `imi_rate` / `ir_rate` | Porcentajes aplicados. |
| `retentions_applied` | `true` si se aplicaron retenciones. |

El generador de documento de pago lee este bloque y calcula:

```text
NetToPay = service_amount + vat - income_tax - municipal_tax
```

---

## Respuestas

### 200 OK

Proceso exitoso. Retorna `true` / cuerpo según contrato del controller.

### 400 Bad Request

| Situación | Código |
|---|---|
| Rol Operator o Supervisor | `ERP:INVALID_ACCESS` |
| Estado inválido | `ERP:INVALID_STATUS_CHANGE` |
| Sin cotizaciones aceptadas | `ERP:NO_ACCEPTED_QUOTATIONS` |

### 404 Not Found

Revisión no encontrada o no está `Pending` → `ERP:MANAGEMENT_REVIEW_NOT_FOUND`.

---

## Roles

| Rol | ¿Puede procesar? |
|---|---|
| Administrador | Sí |
| Manager | Sí |
| Operator | No |
| Supervisor | No |

---

## Códigos de estado

| Código | Descripción |
|:---:|---|
| `200` | Procesado exitosamente. |
| `400` | Validación o regla de negocio. |
| `404` | Revisión no encontrada. |
| `500` | Error interno. |

---

## Checklist frontend antes de llamar

1. La revisión de gerencia está en `Pending`.
2. Cada ítem relevante tiene al menos una cotización con `is_accepted_for_purchase = true`.
3. El usuario es Manager o Admin.
4. Mostrar resumen de totales e indicar si se aplicarán retenciones (total ≥ 1000 y supplier no exento).
