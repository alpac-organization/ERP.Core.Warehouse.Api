# Salidas de Inventario

## Confirmar Despacho

Endpoint para confirmar el despacho de una salida de inventario que se encuentra en estado `InProgress`. Descuenta el stock de cada producto, registra los movimientos de kardex y envía alerta de stock bajo cuando corresponda. Requiere autenticación con token.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/inventory-outputs/{inventory_output_id}/confirm-dispatch` |
| **Descripción** | Confirma el despacho de una salida de inventario en proceso, actualizando el stock y el kardex. Requiere autenticación con token. |

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

No requiere body.

---

## Respuestas

### 204 No Content

Se confirmó el despacho correctamente. Sin cuerpo de respuesta.

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
| `ERP:INVENTORY_OUTPUT_NOT_IN_PROGRESS` | `La salida no está en proceso de despacho.` |
| `ERP:INVALID_ACCESS` | `No tiene acceso para realizar esta acción.` |

### 404 Not Found

La salida de inventario no existe.

### 500 Internal Server Error

Error no controlado del servidor.

---

## Notas

| Regla | Descripción |
|-------|-------------|
| Estado requerido | La salida debe estar en `InProgress`. |
| Efecto | `InProgress → Dispatched`. |
| Stock | Se descuenta `DispatchedQuantity` de cada `ProductStock` asociado. |
| Estado del stock | Se recalcula a `Empty`, `LowBalance` o `Stable` según la cantidad restante y el mínimo. |
| Kardex | Se registra un movimiento de kardex por cada ítem despachado. |
| Alerta de stock bajo | Si algún producto queda en `LowBalance` o `Empty`, se envía un correo al usuario autenticado (si tiene email configurado). |
| Auditoría | Se registra `DispatchedAt` y `DispatchedByUserId`. |
| Roles permitidos | `Administrator` y `Supervisor`. |
