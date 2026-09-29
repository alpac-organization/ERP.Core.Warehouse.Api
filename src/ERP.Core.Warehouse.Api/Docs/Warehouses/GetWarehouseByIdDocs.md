# Almacenes

## Obtener Detalle de Almacén

Endpoint para obtener el detalle de un almacén por id, incluyendo ubicación y capacidad.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}` |
| **Descripción** | Retorna datos del almacén, su ubicación y capacidad completa. Las secciones se consultan por sus endpoints dedicados. |

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

## Respuesta 200 OK (`WarehouseDetailDto`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `warehouse_id` | `guid` | Id del almacén. |
| `code` | `string` | Código. |
| `is_active` | `bool` | Estado. |
| `warehouse_type` | `enum` | Tipo. |
| `location` | `object \| null` | Ubicación (`location_name`). |
| `capacity` | `object \| null` | Capacidad (mismo contrato que el GET capacities). |

```json
{
  "warehouse_id": "1d9c7387-c1eb-45ec-8ff9-a758e410dd7d",
  "code": "WH-001",
  "is_active": true,
  "warehouse_type": "Fiscal",
  "location": {
    "location_name": "Almacenadora del Pacífico - Managua"
  },
  "capacity": {
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
}
```

---

## Errores

### 401 Unauthorized

`user_id` inválido o ausente en el contexto.

### 400 Bad Request

Almacén no encontrado (`ERP:WAREHOUSE_NOT_FOUND`) u otros errores de negocio.

### 500 Internal Server Error

Error no controlado del servidor.
