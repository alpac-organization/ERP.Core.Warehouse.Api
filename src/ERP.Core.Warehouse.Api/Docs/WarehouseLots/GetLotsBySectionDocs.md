## Listar Tramos

Endpoint para listar con paginación y filtros los tramos registrados dentro de una sección de un almacén, en una compañía y módulo.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots` |
| **Descripción** | Retorna un listado paginado de tramos asociados a la sección del almacén, con filtros opcionales de búsqueda. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `sections_id` | `guid` | Sí | Identificador único de la sección. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Query Params

| Parámetro | Tipo | Requerido | Default | Descripción |
|---|---|:---:|:---:|---|
| `code` | `string` | No | `null` | Filtra por coincidencia parcial en el código del tramo. |
| `status` | `enum (RackStatus)` | No | `null` | Filtra por estado del tramo: `Available`, `Occupied`, `UnderMaintenance`, `Blocked`. |
| `page_number` | `integer` | No | `1` | Número de página a consultar. |
| `page_size` | `integer` | No | `10` | Cantidad de registros por página. |

---

## Respuestas

### ✅ 200 OK

Retorna un objeto `PagedResponse<LotListItemDto>`:

```json
{
  "data": [
    {
      "id": "56487c1b-9f4d-4b2a-8e1c-1234567890ab",
      "code": "LOT-001",
      "status": "Available",
      "allows_stacking": true,
      "unavailable_reason": null,
      "status_changed_at": "2026-09-09 10:00:00"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "La sección indicada no existe o no está activa.",
    "error_codes": ["ERP:SECTION_NOT_FOUND"]
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

---

## Catálogos de Enums Utilizados

### `RackStatus`
| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Available` | Listo y libre para asignar mercadería. |
| `2` | `Occupied` | Tiene mercadería asignada actualmente. |
| `3` | `UnderMaintenance` | Fuera de servicio por mantenimiento o reparación estructural. |
| `4` | `Blocked` | Inhabilitado por otra causa. |