## Obtener Capacidades de Tramo

Endpoint para consultar las capacidades de un tramo específico dentro de una sección de un almacén, en una compañía y módulo.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}/capacities` |
| **Descripción** | Retorna las dimensiones y áreas de capacidad del tramo indicado. |

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

### ✅ 200 OK

Retorna un objeto `LotCapacitiesDto`:

```json
{
  "lots_id": "56487c1b-9f4d-4b2a-8e1c-1234567890ab",
  "width": 10.00,
  "length": 8.00,
  "total_area_m2": 80.00,
  "available_area_with_margin_m2": 75.00,
  "unused_area_m2": 5.00,
  "unoccupied_chargeable_area_m2": 70.00,
  "occupied_chargeable_area_m2": 10.00,
  "percentage_available_area_with_margin_m2": 87.50
}
```

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