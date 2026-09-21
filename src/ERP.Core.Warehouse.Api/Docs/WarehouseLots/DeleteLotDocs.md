## Eliminar Tramo

Endpoint para eliminar (soft delete) un tramo específico dentro de una sección de un almacén, en una compañía y módulo.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `DELETE` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}` |
| **Descripción** | Elimina lógicamente el tramo indicado, marcando su fecha de eliminación junto con la de su capacidad y la de **todas sus posiciones** (`lots_positions`), y actualiza en cascada las capacidades de la sección y del almacén recalculadas sin el tramo eliminado. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `sections_id` | `guid` | Sí | Identificador único de la sección. |
| `lot_id` | `guid` | Sí | Identificador único del tramo. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### ✅ 204 No Content

El tramo fue eliminado correctamente. No se retorna cuerpo en la respuesta.

> **Nota:** se aplica `soft delete` (marca `deleted_at`) sobre el tramo, su capacidad (`lots_capacities`) y cada una de sus posiciones (`lots_positions`). Las posiciones eliminadas dejan de aparecer en las consultas de posiciones disponibles. Al eliminar el tramo se recalculan en cascada las capacidades de la sección y del almacén, excluyendo los valores del tramo eliminado.
>
> Si la sección no tiene `section_capacity` registrada, **no se bloquea la petición**: se recalculan los campos que sí pueden determinarse (derivados de las capacidades de los tramos restantes: áreas usada/desocupada, facturables, etc.); `width`, `length` y `TotalAreaM2` no son determinables y se entregan en `0`.

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

**Reglas de negocio:**

| Código | Descripción |
|---|---|
| `ERP:LOT_NOT_FOUND` | El tramo no fue encontrado o no pertenece a la sección indicada. |
| `ERP:SECTION_NOT_FOUND` | La sección no existe o no está activa. |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | La sección no pertenece al almacén indicado. |
| `ERP:LOT_HAS_ACTIVE_STOCK` | No se puede eliminar el tramo porque alguna de sus posiciones tiene **stock activo** (colocación de stock no vaciada). |

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "No se puede eliminar el tramo porque tiene stock activo en sus posiciones.",
    "error_codes": ["ERP:LOT_HAS_ACTIVE_STOCK"]
  },
  "created_at": "2026-09-09 10:00:00"
}
```

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud"
  },
  "created_at": "2026-09-09 10:00:00"
}
```