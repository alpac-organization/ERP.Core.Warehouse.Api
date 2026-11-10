# Salidas de Inventario

## Listar Salidas de Inventario

Endpoint para obtener la lista paginada de salidas de inventario.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/inventory-outputs` |
| **Descripción** | Retorna las salidas de inventario paginadas con filtros opcionales. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Parámetros de Consulta (Query Params)

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `output_code` | `string` | No | - | Filtro por código de salida (contiene). |
| `status` | `enum (InventoryOutputsStatus)` | No | - | Filtro por estado de la salida (`OutputPending`, `Dispatched`). |
| `request_type` | `string` | No | - | Filtro por tipo de solicitud asociada: `Supply` o `Fuel`. |
| `supply_request_code` | `string` | No | - | Filtro por código de la solicitud de insumos asociada. |
| `fuel_request_code` | `string` | No | - | Filtro por código de la solicitud de combustible asociada. |
| `reason_inventory_removal_id` | `guid` | No | - | Filtro por motivo de salida de inventario. |
| `page_number` | `int` | No | `1` | Número de página. |
| `page_size` | `int` | No | `10` | Tamaño de página (máx. recomendado 100). |

> **Restricción por rol:** este endpoint solo puede ser consumido por los roles `Administrator` y `Supervisor`. Otros roles reciben `400` con `ERP:INVALID_ACCESS`.

> **Ordenamiento:** por `created_at` descendente (más reciente primero).

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Lista paginada de salidas de inventario.

```json
{
  "items": [
    {
      "inventory_output_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "output_code": "SAL-00001",
      "observations": "Salida generada al aprobar la solicitud",
      "status": "OutputPending",
      "request_code": "FR-00001",
      "request_type": "Fuel",
      "reasons_inventory_removal_description": "Salida operativa",
      "work_area_name": "Mantenimiento",
      "cost_center_name": "Centro de Costos General",
      "request_date": "2026-11-05 08:00:00"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total_pages": 1,
  "total_records": 1
}
```

**Campos de `GetInventoryOutputDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `inventory_output_id` | `guid` | Identificador único de la salida de inventario. |
| `output_code` | `string` | Código de la salida de inventario. |
| `observations` | `string \| null` | Observaciones de la salida. |
| `status` | `enum (InventoryOutputsStatus)` | Estado de la salida. |
| `request_code` | `string \| null` | Código de la solicitud de insumos o combustible asociada. |
| `request_type` | `string \| null` | Tipo de solicitud asociada: `Supply` o `Fuel`. |
| `reasons_inventory_removal_description` | `string \| null` | Descripción del motivo de salida. |
| `work_area_name` | `string` | Nombre del área de trabajo. |
| `cost_center_name` | `string` | Nombre del centro de costos. |
| `request_date` | `datetime \| null` | Fecha de la solicitud asociada. |

### 400 Bad Request

Error de validación o acceso.

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-11-06 08:00:00"
}
```

### 500 Internal Server Error

Error no controlado del servidor.