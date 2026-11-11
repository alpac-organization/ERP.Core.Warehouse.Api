# Salidas de Inventario

## Iniciar Despacho

Endpoint para iniciar el despacho de una salida de inventario que se encuentra en estado `OutputPending`. Registra las cantidades a despachar por stock de producto y cambia el estado a `InProgress`. Requiere autenticación con token.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/inventory-outputs/{inventory_output_id}/start-dispatch` |
| **Descripción** | Inicia el despacho de una salida de inventario pendiente, registrando las cantidades a despachar por stock de producto. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `inventory_output_id` | `guid` | Sí | Identificador único de la salida de inventario. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |
| `Content-Type` | `application/json` | Sí |

---

## Body

### Despacho completo (default)

```json
{
  "is_all_dispached": true
}
```

### Despacho parcial

```json
{
  "is_all_dispached": false,
  "dispatched_justification": "Despacho parcial por falta de stock",
  "items": [
    {
      "product_stock_id": "2be8dc6a-8dac-48c7-9b66-5ce7a27dd710",
      "dispatched_quantity": 50
    }
  ]
}
```

**Campos del body (`StartInventoryOutputCommand`):**

| Campo | Tipo | Requerido | Default | Descripción |
|-------|------|-----------|---------|-------------|
| `is_all_dispached` | `bool` | No | `true` | Si es `true`, despacha la totalidad de las cantidades solicitadas. Si es `false`, se deben indicar las cantidades en `items` y la justificación. |
| `dispatched_justification` | `string \| null` | Condicional | - | Justificación del despacho. Requerida cuando `is_all_dispached` es `false`. Si no se envía en despacho completo, se asigna `"Despacho completo"`; en parcial, `"Despacho parcial"`. |
| `items` | `array (StartInventoryOutputItem)` | Condicional | `[]` | Cantidades a despachar por stock de producto. Requerido y no vacío cuando `is_all_dispached` es `false`. |

**Campos de `StartInventoryOutputItem`:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `product_stock_id` | `guid` | Sí | Identificador del stock de producto a despachar. Debe pertenecer a la salida. |
| `dispatched_quantity` | `decimal` | Sí | Cantidad a despachar. Debe ser mayor a cero y no superar la cantidad solicitada. |

---

## Respuestas

### 204 No Content

Se inició el despacho correctamente. Sin cuerpo de respuesta.

### 400 Bad Request

Error de validación o acceso.

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-11-06 08:00:00"
}
```

Errores específicos del handler:

| `type_error` | `description` |
|--------------|---------------|
| `ERP:INVENTORY_OUTPUT_IN_PROGRESS` | `La salida ya está en proceso de despacho.` |
| `ERP:INVENTORY_OUTPUT_NOT_PENDING` | `La salida no está pendiente para iniciar despacho.` |
| `ERP:INVALID_OUTPUT_ITEM` | `El ítem no pertenece a la salida.` |
| `ERP:INVALID_DISPATCH_QUANTITY` | `La cantidad a despachar supera la solicitada.` |
| `ERP:INVALID_ACCESS` | `No tiene acceso para realizar esta acción.` |

### 404 Not Found

La salida de inventario no existe.

### 500 Internal Server Error

Error no controlado del servidor.

---

## Notas

| Regla | Descripción |
|-------|-------------|
| Estado requerido | La salida debe estar en `OutputPending`. |
| Efecto | `OutputPending → InProgress`. |
| Despacho completo | Si `is_all_dispached = true`, se despacha la totalidad de las cantidades solicitadas por cada stock. |
| Despacho parcial | Si `is_all_dispached = false`, las cantidades indicadas en `items` deben ser mayores a cero y no superar las solicitadas. Se exige justificación. |
| Re-ejecución | Bloqueada: si ya está `InProgress` responde `ERP:INVENTORY_OUTPUT_IN_PROGRESS`. |
| Roles permitidos | `Administrator` y `Supervisor`. |
