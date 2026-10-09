# Cotizaciones

## Aceptar Cotización para Compra

Endpoint para marcar una cotización como aceptada para compra dentro de un ítem de solicitud. Requiere que el ítem tenga al menos dos cotizaciones activas (comparativa).

| Campo | Valor |
|---|---|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/quotations/{quotation_id}/accept-for-purchase` |
| **Descripción** | Marca `is_accepted_for_purchase = true` en la cotización seleccionada y gestiona el resto de cotizaciones del mismo ítem según las reglas del handler. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo. |
| `quotation_id` | `guid` | Sí | Cotización a aceptar. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `purchase_request_item_id` | `guid` | Sí | Ítem de la solicitud al que pertenece la cotización. |
| `supplier_selection_justification` | `string` | No | Justificación de la selección. |
| `supplier_rejection_justification` | `string` | No | Comentarios de rechazo de otras opciones. |

### Ejemplo

```json
{
  "purchase_request_item_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "supplier_selection_justification": "Mejor relación precio-entrega",
  "supplier_rejection_justification": "Otros proveedores con mayor lead time"
}
```

---

## Respuestas

### 200 OK

Cotización aceptada. Cuerpo vacío.

### 400 Bad Request

Reglas de negocio (p. ej. menos de 2 cotizaciones activas, rol Supervisor).

### 404 Not Found

Cotización o ítem no encontrado.

---

## Notas para frontend

- Antes de aceptar, mostrar la matriz comparativa de cotizaciones del ítem.
- Solo las cotizaciones con `is_accepted_for_purchase = true` se usan al generar la orden de compra.
- Flujo siguiente: enviar la solicitud a revisión contable.

---

## Códigos de estado

| Código | Descripción |
|:---:|---|
| `200` | Aceptada exitosamente. |
| `400` | Validación o regla de negocio. |
| `404` | No encontrado. |
| `500` | Error interno. |
