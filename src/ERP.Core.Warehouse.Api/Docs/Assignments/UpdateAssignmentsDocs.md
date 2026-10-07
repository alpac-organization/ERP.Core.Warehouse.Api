# Asignaciones Operativas

## Actualizar Asignación Operativa

Endpoint para actualizar los datos de una asignación operativa existente.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}` |
| **Descripción** | Actualiza campos editables de la asignación: observaciones, mercancía, descripción, almacén destino y tipo de destino. `assignment_id` y `operational_order_id` se toman de la ruta (no se envían en el body). |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `operational_order_id` | `guid` | Sí | Identificador único de la orden operativa. |
| `assignment_id` | `guid` | Sí | Identificador único de la asignación. |

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
| `observations` | `string` | No | Observaciones actualizadas. |
| `merchandise` | `string` | No | Nombre de la mercancía actualizado. |
| `merchandiseDescription` | `string` | No | Descripción de la mercancía actualizada. |
| `warehouseId` | `guid` | No | Identificador del almacén destino (si `destinationType` es `Warehouse`). |
| `destinationType` | `enum (DestinationType)` | No | Tipo de destino: `None` (0), `Warehouse` (1), `CustomYard` (2), `CustomSheld` (3). |

### Ejemplo de Request

```json
{
  "observations": "Actualización: contenedor revisado",
  "merchandise": "Contenedor 40ft - Revisado",
  "merchandiseDescription": "Contenedor con mercancía paletizada, verificado",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "destinationType": 1
}
```

---

## Respuestas

### 204 No Content

Asignación actualizada correctamente. Sin cuerpo de respuesta.

### 400 Bad Request

Validaciones de entrada, asignación no encontrada, almacén no existe, sin permiso, etc. (`ErrorResponse`).

### 500 Internal Server Error

Error no controlado del servidor.