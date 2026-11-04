# Solicitudes Operativas de Inventario

## Listar Solicitudes de Inventario

Endpoint para obtener la lista paginada de solicitudes operativas de inventario.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/supply-requests` |
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
| `code` | `string` | No | - | Filtrar por código de la solicitud de inventario (contiene). |
| `branch_id` | `guid` | No | - | Filtrar por identificador de la sucursal. |
| `po_code` | `string` | No | - | Filtrar por código de la orden operativa asociada (contiene). |
| `reason_use` | `enum (ReasonUse)` | No | - | Filtrar por motivo de uso de los ítems de la solicitud. |
| `status` | `enum (OutflowStatus)` | No | - | Filtrar por estado de la solicitud de salida. |
| `page_number` | `int` | No | `1` | Número de página. |
| `page_size` | `int` | No | `10` | Tamaño de página (máx. recomendado 100). |

> **Restricción por rol:** este endpoint solo puede ser consumido por los roles `Administrator` y `Supervisor`. Otros roles reciben `400` con `ERP:INVALID_ACCESS`.
>
> **Regla temporal:** se retornan todas las solicitudes con estado `PendingReview` (sin límite de fecha); los demás estados solo si fueron creadas en los últimos 7 días.
>
> **Ordenamiento:** por `created_at` descendente (más reciente primero).

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
      "po_code": "PO-00001",
      "reason_use": "Maintenance",
      "justification": "Repuesto necesario para mantenimiento",
      "outflow_status": "Pending",
      "requested_by_user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "requested_by_user_name": "Juan Pérez",
      "work_area_name": "Mantenimiento",
      "warehouse_code": "BOD-001",
      "created_at": "2026-10-27 08:00:00",
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
| `po_code` | `string` | Código de la orden operativa asociada. |
| `reason_use` | `enum (ReasonUse)` | Motivo de uso de la solicitud. |
| `justification` | `string \| null` | Justificación de la solicitud. |
| `outflow_status` | `enum (OutflowStatus)` | Estado de la solicitud de salida. |
| `requested_by_user_id` | `guid` | Identificador del usuario solicitante. |
| `requested_by_user_name` | `string` | Nombre del usuario solicitante. |
| `work_area_name` | `string` | Nombre del área de trabajo. |
| `warehouse_code` | `string` | Código de la bodega destino. |
| `created_at` | `datetime` | Fecha de creación de la solicitud. |
| `items_count` | `int` | Cantidad de ítems en la solicitud. |

### 400 Bad Request

Error de validación o acceso (parámetros inválidos, paginación incorrecta, rol no autorizado, etc.).

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
