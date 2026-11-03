# Asignaciones Operativas

## Listar Asignaciones Operativas

Endpoint para obtener la lista paginada de asignaciones operativas de una orden operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/assignments` |
| **Descripción** | Retorna las asignaciones operativas paginadas con filtros opcionales. Requiere autenticación con token. |

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
| `operational_order_id` | `guid` | No | - | Identificador único de la orden operativa. Si se omite, retorna las asignaciones de todas las órdenes accesibles. |
| `page_number` | `int` | No | `1` | Número de página. |
| `page_size` | `int` | No | `10` | Tamaño de página (máx. recomendado 100). |
| `status` | `enum (AssignmentOperationalStatus)` | No | - | Filtrar por estado de la asignación. Valores: `None` (0), `Pending` (1), `InProgress` (2), `OnHold` (3), `Downloaded` (4). |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Lista paginada de asignaciones operativas. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`).

```json
{
  "data": [
    {
      "assignment_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "operational_order_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "reception_code": "REC-2026-0001",
      "po_code": "PO-2026-0001",
      "is_alerted": false,
      "destination_type": "Warehouse",
      "status": "Pending",
      "merchandise": "Mercancía A",
      "merchandise_description": "Descripción de la mercancía",
      "has_machinery_assigned": true,
      "has_collaborators_assigned": false,
      "has_outsourced_crew": true,
      "has_outsourced_machinery": false,
      "created_at": "2025-01-15T10:30:00Z"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 50
}
```

**Wrapper (`PagedResponse<AssignmentOperationalDto>`):**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `data` | `array[AssignmentOperationalDto]` | Listado de asignaciones de la página. |
| `page_number` | `int` | Número de página devuelto. |
| `page_size` | `int` | Tamaño de página usado. |
| `total` | `int` | Total de registros que cumplen los filtros. |

**Campos de `AssignmentOperationalDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `assignment_id` | `guid` | Identificador único de la asignación. |
| `operational_order_id` | `guid` | Identificador de la orden operativa padre. |
| `reception_code` | `string` | Código de la recepción asociada a la orden operativa. Puede ser `null`. |
| `po_code` | `string` | Código de la orden de compra (`PoCode`) de la orden operativa. Puede ser `null`. |
| `is_alerted` | `bool` | Indica si la orden operativa tiene alerta. |
| `destination_type` | `enum (DestinationType)` | Tipo de destino: `None` (0), `Warehouse` (1), `CustomYard` (2), `CustomSheld` (3). |
| `status` | `enum (AssignmentOperationalStatus)` | Estado: `None` (0), `Pending` (1), `InProgress` (2), `OnHold` (3), `Downloaded` (4). |
| `merchandise` | `string` | Nombre de la mercancía. |
| `merchandise_description` | `string` | Descripción de la mercancía. |
| `has_machinery_assigned` | `bool` | Indica si tiene maquinaria propia asignada. |
| `has_collaborators_assigned` | `bool` | Indica si tiene colaboradores propios asignados. |
| `has_outsourced_crew` | `bool` | Indica si cuenta con personal externo (cuadrilla). |
| `has_outsourced_machinery` | `bool` | Indica si cuenta con maquinaria externa. |
| `created_at` | `datetime` | Fecha de creación (UTC). |

### 400 Bad Request

Error de validación (parámetros inválidos, paginación incorrecta, etc.).

### 500 Internal Server Error

Error no controlado del servidor.