## Eliminar Tramo

Endpoint para eliminar (soft delete) un tramo específico dentro de una sección de un almacén, en una compañía y módulo.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `DELETE` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}` |
| **Descripción** | Elimina lógicamente el tramo indicado, marcando su fecha de eliminación junto con la de su capacidad, y actualiza en cascada las capacidades de la sección y del almacén recalculadas sin el tramo eliminado. |

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

> **Nota:** al eliminar el tramo se recalculan en cascada las capacidades de la sección y del almacén, excluyendo los valores del tramo eliminado.

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El tramo no fue encontrado.",
    "error_codes": ["ERP:LOT_NOT_FOUND"]
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