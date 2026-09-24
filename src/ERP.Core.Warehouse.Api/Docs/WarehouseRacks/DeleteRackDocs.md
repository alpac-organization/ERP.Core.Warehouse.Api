## Eliminar Rack (Soft Delete y Recálculo en Cascada)

Endpoint para realizar la eliminación lógica (*soft delete*) de un rack específico perteneciente a una sección de un almacén.

---

## Información General

| Campo | Valor |
|---|---|
| **Método** | `DELETE` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks/{rack_id}` |
| **Descripción** | Valida que ninguna posición del rack posea mercadería activa, marca fecha de eliminación lógica en cascada (`racks`, `rack_positions`, `racks_capacities`, `racks_coordinates`) y recalcula las capacidades de la sección y del almacén sin el rack eliminado. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `section_id` | `guid` | Sí | Identificador único de la sección. |
| `rack_id` | `guid` | Sí | Identificador único del rack a eliminar. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Reglas de Negocio e Integridad

1. **Bloqueo por Stock Activo:**
   Antes de proceder a la eliminación, el sistema consulta todas las posiciones físicas activas del rack contra la tabla `stock_placements`. Si se detecta al menos un registro con `vacated_at_date IS NULL` y `deleted_at IS NULL`, la eliminación se cancela de inmediato retornando HTTP 400 (`ERP:RACK_HAS_ACTIVE_STOCK`).
2. **Eliminación Lógica (*Soft Delete*):**
   Se marca `deleted_at = DateTime.UtcNow` en:
   - La entidad principal `racks`.
   - Todas las posiciones asociadas en `rack_positions`.
   - La capacidad volumétrica en `racks_capacities`.
   - Las coordenadas espaciales en `racks_coordinates`.
3. **Recálculo de Capacidad en Cascada:**
   Se calcula la nueva capacidad de la sección excluyendo el rack eliminado mediante `IRackCapacityCalculator.DeleteRackAsync(rackId, ...)`, impactando las tablas `section_capacities` y `warehouse_capacities`.

---

## Respuestas

### ✅ 204 No Content

El rack, sus posiciones y entidades hijas fueron marcados como eliminados correctamente y las capacidades fueron actualizadas en cascada. La respuesta es un código HTTP `204 No Content` sin cuerpo.

---

### ❌ 400 Bad Request

Se emite cuando el rack contiene stock activo o cuando los identificadores no existen o no concuerdan:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "No se puede eliminar el rack porque tiene stock activo asignado en sus posiciones.",
    "error_codes": ["ERP:RACK_HAS_ACTIVE_STOCK"]
  },
  "created_at": "2026-09-23 17:40:00"
}
```

#### Catálogo de Errores:
| Código de Error | Descripción |
|---|---|
| `ERP:RACK_HAS_ACTIVE_STOCK` | No es posible eliminar el rack debido a que una o más de sus posiciones albergan mercadería activa sin desocupar. |
| `ERP:RACK_NOT_FOUND` | El rack no existe, ya fue eliminado o no pertenece a la sección indicada. |
| `ERP:SECTION_NOT_FOUND` | La sección indicada no existe o no se encuentra activa. |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | La sección no pertenece al almacén especificado en la ruta. |

---

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud."
  },
  "created_at": "2026-09-23 17:40:00"
}
```
