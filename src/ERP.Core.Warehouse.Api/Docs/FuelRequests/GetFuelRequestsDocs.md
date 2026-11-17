# Solicitudes de Combustible

## Listar Solicitudes de Combustible

Endpoint para obtener la lista paginada de solicitudes de combustible.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/fuel-requests` |
| **Descripción** | Retorna las solicitudes de combustible paginadas con filtros opcionales. Requiere autenticación con token. |

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
| `code` | `string` | No | - | Filtrar por código de la solicitud de combustible (contiene). |
| `branch_id` | `guid` | No | - | Filtrar por identificador de la sucursal. |
| `equipment_id` | `guid` | No | - | Filtrar por identificador del equipo. |
| `product_stock_id` | `guid` | No | - | Filtrar por identificador del stock de combustible. |
| `status` | `enum (OutflowStatus)` | No | - | Filtrar por estado de la solicitud. |
| `page_number` | `int` | No | `1` | Número de página. |
| `page_size` | `int` | No | `10` | Tamaño de página (máx. recomendado 100). |

> **Restricción por rol:** este endpoint puede ser consumido por los roles `Administrator`, `Supervisor` y `Operator`. `Administrator` y `Supervisor` ven solicitudes de su branch y área; `Operator` ve solo las solicitudes creadas por él mismo (por branch). Otros roles reciben `400` con `ERP:INVALID_ACCESS`.
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

Lista paginada de solicitudes de combustible.

```json
{
  "data": [
    {
      "fuel_request_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fuel_request_code": "FR-00001",
      "outflow_status": "PendingReview",
      "equipment_code": "EQ-001",
      "equipment_brand": "Toyota",
      "equipment_model": "7FDU35",
      "requested_by_user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "requested_by_user_name": "Juan Pérez",
      "created_at": "2026-11-05 08:00:00"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total_pages": 1,
  "total_records": 1
}
```

**Campos de `GetFuelRequestDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `fuel_request_id` | `guid` | Identificador único de la solicitud de combustible. |
| `fuel_request_code` | `string` | Código de la solicitud de combustible. |
| `outflow_status` | `enum (OutflowStatus)` | Estado de la solicitud. |
| `equipment_code` | `string` | Código del equipo o vehículo. |
| `equipment_brand` | `string` | Marca del equipo. |
| `equipment_model` | `string` | Modelo del equipo. |
| `requested_by_user_id` | `guid` | Identificador del usuario solicitante. |
| `requested_by_user_name` | `string` | Nombre del usuario solicitante. |
| `created_at` | `datetime` | Fecha de creación de la solicitud. |

### 400 Bad Request

Error de validación o acceso.

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-11-05 08:00:00"
}
```

### 500 Internal Server Error

Error no controlado del servidor.