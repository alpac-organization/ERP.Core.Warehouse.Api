# Almacenes

## Obtener Capacidades de Almacén

Endpoint dedicado para consultar la capacidad persistida de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/capacities` |
| **Descripción** | Retorna dimensiones, márgenes y métricas de área/volumen del `warehouse_capacity`. No recalcula: lee lo persistido. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo. |
| `warehouse_id` | `guid` | Sí | Identificador del almacén. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuesta 200 OK (`WarehouseCapacitiesDto`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `width` / `length` | `decimal` | Dimensiones. |
| `has_margins` | `bool` | Si aplica márgenes. |
| `minimum_height` / `maximum_height` | `decimal?` | Alturas. |
| `margin_top` / `margin_bottom` / `margin_left` / `margin_right` | `decimal?` | Márgenes. |
| `total_area_m2` | `decimal` | Área total. |
| `unused_area_m2` | `decimal` | Área inutilizable (márgenes). |
| `available_area_with_margin_m2` | `decimal` | Área útil. |
| `occupied_chargeable_area_m2` | `decimal` | Área facturable ocupada. |
| `unoccupied_chargeable_area_m2` | `decimal` | Área facturable libre. |
| `percentage_available_area_with_margin_m2` | `decimal` | % área disponible. |
| `total_volumen_m3` y campos `*_volumen_m3` | `decimal?` | Métricas de volumen. |
| `percentage_available_volumen_with_margin_m3` | `decimal` | % volumen disponible. |

> No se devuelve `warehouse_id` en el body: ya viene en la ruta.

```json
{
  "width": 50.00,
  "length": 40.00,
  "has_margins": true,
  "minimum_height": 0.00,
  "maximum_height": 8.00,
  "margin_top": 1.00,
  "margin_bottom": 1.00,
  "margin_left": 1.00,
  "margin_right": 1.00,
  "total_area_m2": 2000.00,
  "unused_area_m2": 176.00,
  "available_area_with_margin_m2": 1824.00,
  "occupied_chargeable_area_m2": 0.00,
  "unoccupied_chargeable_area_m2": 0.00,
  "percentage_available_area_with_margin_m2": 91.20,
  "total_volumen_m3": 16000.00,
  "unused_volumen_m3": 0.00,
  "available_volumen_with_margin_m3": 16000.00,
  "occupied_chargeable_volumen_m3": 0.00,
  "unoccupied_chargeable_volumen_m3": 0.00,
  "percentage_available_volumen_with_margin_m3": 100.00
}
```

---

## Errores

### 400 Bad Request

| Código típico | Descripción |
|---------------|-------------|
| `ERP:WAREHOUSE_NOT_FOUND` | El almacén no existe. |
| `ERP:WAREHOUSE_CAPACITY_NOT_FOUND` | El almacén no tiene capacidad registrada. |

### 500 Internal Server Error

Error no controlado del servidor.
