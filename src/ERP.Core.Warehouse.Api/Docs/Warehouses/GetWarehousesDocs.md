# Almacenes

## Listar Almacenes (paginado)

Endpoint para obtener el listado paginado de almacenes de una compañía/módulo.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse` |
| **Descripción** | Devuelve almacenes no eliminados (`deleted_at` null), con filtros opcionales y paginación. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Query Params

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `warehouse_code` | `string` | No | Filtra por código exacto del almacén. |
| `warehouse_type` | `enum (WarehouseType)` | No | Filtra por tipo de almacén. |
| `is_active` | `bool` | No | Filtra por estado activo/inactivo. |
| `page_number` | `int` | No | Página (default `1`). Debe ser > 0. |
| `page_size` | `int` | No | Tamaño de página (default `10`). Debe ser > 0 y ≤ máximo del validator. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuesta 200 OK

`PagedResponse<WarehouseDto>`:

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `data` | `array` | Lista de almacenes. |
| `page_number` | `int` | Página actual. |
| `page_size` | `int` | Tamaño de página. |
| `total` | `int` | Total de registros. |

### Item (`WarehouseDto`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `warehouse_id` | `guid` | Id del almacén. |
| `code` | `string` | Código. |
| `is_active` | `bool` | Estado. |
| `warehouse_type` | `enum` | Tipo. |

```json
{
  "data": [
    {
      "warehouse_id": "1d9c7387-c1eb-45ec-8ff9-a758e410dd7d",
      "code": "WH-001",
      "is_active": true,
      "warehouse_type": "Fiscal"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

---

## Errores

### 400 Bad Request

Paginación inválida, enum inválido o fallo de acceso.

### 500 Internal Server Error

Error no controlado del servidor.
