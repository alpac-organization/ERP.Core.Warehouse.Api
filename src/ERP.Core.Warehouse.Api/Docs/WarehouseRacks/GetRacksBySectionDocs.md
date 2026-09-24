## Listar Racks por Sección (Tabla y Plano 2D)

Endpoint para consultar con paginación y filtros el listado de racks registrados dentro de una sección específica de un almacén. Proporciona las dimensiones, perfiles de uso, métricas de ocupación y las coordenadas cartesianas para renderizar los módulos directamente en el lienzo/canvas 2D del visualizador de almacén.

---

## Información General

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks` |
| **Descripción** | Retorna un listado paginado de racks con su capacidad volumétrica, conteo de posiciones ocupadas/totales y coordenadas cartesianas para visualización 2D. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `section_id` | `guid` | Sí | Identificador único de la sección. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Parámetros de Consulta (Query Params)

| Parámetro | Tipo | Requerido | Default | Descripción |
|---|---|:---:|:---:|---|
| `code` | `string` | No | `null` | Filtra por coincidencia parcial en el código del rack (ej. `"RACK-01"`). |
| `row_number` | `integer` | No | `null` | Filtra por número exacto de hilera física. |
| `level_number` | `integer` | No | `null` | Filtra por cantidad de niveles en altura (ej. `2` o `4`). |
| `status` | `enum (RackStatus)` | No | `null` | Filtra por estado operativo: `Available`, `Occupied`, `UnderMaintenance`, `Blocked`. |
| `usage_profile` | `enum (RackUsageProfile)` | No | `null` | Filtra por perfil operativo: `ActiveFlow`, `StaticHold`. |
| `page_number` | `integer` | No | `1` | Número de página a consultar (base 1). |
| `page_size` | `integer` | No | `24` | Cantidad de registros por página. |

---

## Respuestas

### ✅ 200 OK

Retorna una estructura `PagedResponse<RackListDto>` con la metadata de paginación y la colección de racks:

```json
{
  "data": [
    {
      "id": "e2a48b32-0001-4444-8888-abcdef012345",
      "section_id": "f8a964a3-76a0-4fc7-bf98-251f28b4d081",
      "code": "SECTION_A003-RACK-01",
      "row_number": 1,
      "level_number": 2,
      "max_pulleys": 2,
      "usage_profile": "ActiveFlow",
      "status": "Available",
      "status_changed_at": null,
      "unavailable_reason": null,
      "total_positions": 4,
      "occupied_positions": 0,
      "coordinates": {
        "id": "3b2e591c-1111-4444-9999-012345abcdef",
        "rack_id": "e2a48b32-0001-4444-8888-abcdef012345",
        "position_x": 2.50,
        "position_y": 6.50,
        "position_z": 0.00,
        "rotation_y": 0.00
      },
      "capacity": {
        "id": "a1b2c3d4-2222-4444-aaaa-bbbbccccdddd",
        "rack_id": "e2a48b32-0001-4444-8888-abcdef012345",
        "width_metres": 1.07,
        "length_metres": 2.44,
        "height_metres": 4.50,
        "area_m2": 2.61,
        "volume_m3": 11.75,
        "max_weight_kg": 4000.00
      }
    }
  ],
  "page_number": 1,
  "page_size": 24,
  "total": 24
}
```

---

### ❌ 400 Bad Request

Se emite cuando la sección no existe, está inactiva o no coincide con el almacén indicado en la ruta:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "La sección no pertenece al almacén indicado.",
    "error_codes": ["ERP:SECTION_WAREHOUSE_MISMATCH"]
  },
  "created_at": "2026-09-23 17:32:00"
}
```

#### Catálogo de Errores:
| Código de Error | Descripción |
|---|---|
| `ERP:SECTION_NOT_FOUND` | La sección indicada no existe o no está activa en la base de datos. |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | El identificador de almacén no coincide con el registrado en la sección. |

---

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud."
  },
  "created_at": "2026-09-23 17:32:00"
}
```

---

## Catálogos de Enums Utilizados

### `RackUsageProfile`
| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `ActiveFlow` | Racks de rotación continua y rápida en pasillos principales. |
| `2` | `StaticHold` | Racks de almacenaje estático y resguardo prolongado. |

### `RackStatus`
| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Available` | Disponible para asignación. |
| `2` | `Occupied` | Ocupado por mercadería activa. |
| `3` | `UnderMaintenance` | En mantenimiento. |
| `4` | `Blocked` | Bloqueado administrativamente. |
