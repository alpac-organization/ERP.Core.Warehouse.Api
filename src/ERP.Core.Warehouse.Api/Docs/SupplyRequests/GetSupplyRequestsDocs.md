# Solicitudes Operativas de Inventario

## Listar Solicitudes de Inventario

Endpoint para obtener la lista paginada de solicitudes operativas de inventario.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/supply-requests` |
| **Descripción** | Retorna las solicitudes operativas de inventario paginadas con filtros opcionales. Requiere autenticación con token. |

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
| `code` | `string` | No | - | Filtrar por código de la solicitud de inventario. |
| `work_area_id` | `guid` | No | - | Filtrar por identificador del área de trabajo. |
| `cost_center_id` | `guid` | No | - | Filtrar por identificador del centro de costos. |
| `branch_id` | `guid` | No | - | Filtrar por identificador de la sucursal. |
| `warehouse_id` | `guid` | No | - | Filtrar por identificador del almacén. |
| `operational_order_id` | `guid` | No | - | Filtrar por identificador de la orden operativa. |
| `requested_by_user_id` | `guid` | No | - | Filtrar por identificador del usuario solicitante. |
| `page_number` | `int` | No | `1` | Número de página. |
| `page_size` | `int` | No | `10` | Tamaño de página (máx. recomendado 100). |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Lista paginada de solicitudes operativas de inventario.

```json
{
  "items": [
    {
      "supply_request_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "supply_request_code": "SI-00001",
      "reason_use": "Maintenance",
      "justification": "Repuesto necesario para mantenimiento",
      "outflow_status": "Pending",
      "requested_by_user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "requested_by_user_name": "Juan Pérez",
      "items_count": 3
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total_pages": 1,
  "total_records": 1
}
```

**Campos de `GetSupplyRequestDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `supply_request_id` | `guid` | Identificador único de la solicitud de inventario. |
| `supply_request_code` | `string` | Código de la solicitud de inventario. |
| `reason_use` | `enum (ReasonUse)` | Motivo de uso de la solicitud. |
| `justification` | `string \| null` | Justificación de la solicitud. |
| `outflow_status` | `enum (OutflowStatus)` | Estado de la solicitud de salida. |
| `requested_by_user_id` | `guid` | Identificador del usuario solicitante. |
| `requested_by_user_name` | `string` | Nombre del usuario solicitante. |
| `items_count` | `int` | Cantidad de ítems en la solicitud. |

### 400 Bad Request

Error de validación (parámetros inválidos, paginación incorrecta, etc.).

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-10-27 08:00:00"
}
```

### 500 Internal Server Error

Error no controlado del servidor.
