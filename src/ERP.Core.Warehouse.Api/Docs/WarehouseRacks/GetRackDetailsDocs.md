## Detalle Individual de Rack (Posiciones y Stock Activo)

Endpoint para consultar la información detallada e integral de un rack específico: dimensiones físicas, coordenadas espaciales, capacidades y la matriz completa de sus posiciones físicas (`RackPositions`) con el detalle del producto y stock asignado actualmente en cada polín.

---

## Información General

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks/{rack_id}/details` |
| **Descripción** | Retorna el detalle completo del rack, sus posiciones físicas (desglosadas por nivel y polín) y la información de la mercadería colocada en tiempo real. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `section_id` | `guid` | Sí | Identificador único de la sección. |
| `rack_id` | `guid` | Sí | Identificador único del rack a consultar. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### ✅ 200 OK

Retorna un objeto `RackDetailsDto`:

```json
{
  "id": "e2a48b32-0001-4444-8888-abcdef012345",
  "section_id": "f8a964a3-76a0-4fc7-bf98-251f28b4d081",
  "code": "SECTION_A003-RACK-01",
  "row_number": 1,
  "level_number": 2,
  "max_pulleys": 2,
  "usage_profile": "ActiveFlow",
  "status": "Available",
  "unavailable_reason": null,
  "status_changed_at": null,
  "coordinates": {
    "position_x": 2.50,
    "position_y": 6.50,
    "position_z": 0.00,
    "rotation_y": 0.00
  },
  "capacity": {
    "width_metres": 1.07,
    "length_metres": 2.44,
    "height_metres": 4.50,
    "area_m2": 2.61,
    "volume_m3": 11.75,
    "max_weight_kg": 4000.00
  },
  "positions": [
    {
      "position_id": "a0000001-1111-2222-3333-444455556666",
      "code": "SECTION_A003-RACK-01-N1P1",
      "row": 1,
      "column": 1,
      "level": 1,
      "status": "Occupied",
      "allows_stocking": true,
      "current_stock": {
        "stock_id": "99887766-5544-3322-1100-aabbccddeeff",
        "product_name": "Bobina de Acero Galvanizado 1.5mm",
        "category_name": "MAT-PRIMA",
        "current_weight_kg": 1250.00,
        "current_bultos": 1.00,
        "placed_at_date": "2026-09-20",
        "placed_at_time": "14:30:00"
      }
    },
    {
      "position_id": "a0000002-1111-2222-3333-444455556666",
      "code": "SECTION_A003-RACK-01-N1P2",
      "row": 1,
      "column": 2,
      "level": 1,
      "status": "Available",
      "allows_stocking": true,
      "current_stock": null
    },
    {
      "position_id": "a0000003-1111-2222-3333-444455556666",
      "code": "SECTION_A003-RACK-01-N2P1",
      "row": 1,
      "column": 1,
      "level": 2,
      "status": "Available",
      "allows_stocking": true,
      "current_stock": null
    },
    {
      "position_id": "a0000004-1111-2222-3333-444455556666",
      "code": "SECTION_A003-RACK-01-N2P2",
      "row": 1,
      "column": 2,
      "level": 2,
      "status": "Available",
      "allows_stocking": true,
      "current_stock": null
    }
  ]
}
```

---

### ❌ 400 Bad Request

Se emite cuando la sección o el rack no existen, o hay discordancia en las rutas:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El rack solicitado no existe.",
    "error_codes": ["ERP:RACK_NOT_FOUND"]
  },
  "created_at": "2026-09-23 17:35:00"
}
```

#### Catálogo de Errores:
| Código de Error | Causa |
|---|---|
| `ERP:SECTION_NOT_FOUND` | La sección no fue encontrada o no pertenece al almacén especificado. |
| `ERP:RACK_NOT_FOUND` | El identificador de rack no existe, fue eliminado o no pertenece a la sección indicada. |

---

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud."
  },
  "created_at": "2026-09-23 17:35:00"
}
```
