# Solicitudes Operativas de Inventario

## Registrar Solicitud de Inventario

Endpoint para registrar una nueva solicitud operativa de inventario.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/supply-requests` |
| **Descripción** | Registra una nueva solicitud operativa de inventario con sus ítems. Los valores `company_id`, `module_code` y `user_id` se toman de la ruta y del token respectivamente. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |
| `Content-Type` | `application/json` | Sí |

---

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `justification` | `string` | Sí | Justificación de la solicitud de inventario. |
| `operational_order_id` | `guid` | Sí | Identificador de la orden operativa asociada. |
| `reason_use` | `enum (ReasonUse)` | Sí | Motivo de uso de la solicitud. |
| `items` | `array[RegisterSupplyRequestItem]` | Sí | Listado de ítems a solicitar. |
| `items[].product_stock_id` | `guid` | Sí | Identificador del producto en stock a solicitar. |
| `items[].amount` | `int` | Sí | Cantidad a solicitar. |

### Ejemplo de Request

```json
{
  "justification": "Repuestos necesarios para mantenimiento correctivo",
  "operational_order_id": "3fa85f64-5717-4562-b3fc-2c963f66afb2",
  "reason_use": "Maintenance",
  "items": [
    {
      "product_stock_id": "3fa85f64-5717-4562-b3fc-2c963f66afb4",
      "amount": 2
    },
    {
      "product_stock_id": "3fa85f64-5717-4562-b3fc-2c963f66afb5",
      "amount": 1
    }
  ]
}
```

---

## Respuestas

### 201 Created

Solicitud de inventario registrada correctamente. Retorna `CreatedResult` sin cuerpo.

### 400 Bad Request

Error de validación (parámetros inválidos, validaciones de negocio, etc.).

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-10-27 08:00:00"
}
```

### 500 Internal Server Error

Error no controlado del servidor.
