# Asignaciones Operativas

## Actualizar Asignación Operativa

Endpoint para actualizar los datos de una asignación operativa existente.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}` |
| **Descripción** | Actualiza campos editables de la asignación: observaciones, mercancía, descripción, almacén destino, tipo de destino y los datos de personal/maquinaria externa. `assignment_id` y `operational_order_id` se toman de la ruta (no se envían en el body). |
| **Serialización** | El JSON usa `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`). |

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
| `merchandise_description` | `string` | No | Descripción de la mercancía actualizada. |
| `warehouse_id` | `guid` | No | Identificador del almacén destino (si `destination_type` es `Warehouse`). |
| `destination_type` | `enum (DestinationType)` | No | Tipo de destino: `None` (0), `Warehouse` (1), `CustomYard` (2), `CustomSheld` (3). |
| `outsourced_crew_count` | `int` | No | Cantidad de personal externo. Si se envía con valor `> 0`, el sistema marca `has_outsourced_crew = true`. |
| `outsourced_machinery_supplier` | `string` | No | Proveedor de maquinaria externa. Si se envía no vacío, el sistema marca `has_outsourced_machinery = true`. |

> **Campos externos calculados:** `has_outsourced_crew` y `has_outsourced_machinery` **no** se envían en el body (no son editables directamente). El servidor los **activa** automáticamente:
> - `outsourced_crew_count > 0` → `has_outsourced_crew = true`
> - `outsourced_machinery_supplier` no vacío → `has_outsourced_machinery = true`
>
> Nunca se cambian a `false`; solo se encienden cuando llega el dato correspondiente.

### Ejemplo de Request

```json
{
  "observations": "Actualización: contenedor revisado",
  "merchandise": "Contenedor 40ft - Revisado",
  "merchandise_description": "Contenedor con mercancía paletizada, verificado",
  "warehouse_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "destination_type": "Warehouse",
  "outsourced_crew_count": 4,
  "outsourced_machinery_supplier": "Maquinarias del Norte S.A."
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