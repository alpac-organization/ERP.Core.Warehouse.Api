# Asignaciones Operativas

## Obtener Detalle de Asignación Operativa

Endpoint para obtener el detalle completo de una asignación operativa específica.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/details` |
| **Descripción** | Retorna el detalle completo de la asignación incluyendo observaciones, datos adicionales e información del almacén. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `operational_order_id` | `guid` | Sí | Identificador único de la orden operativa. |
| `assignment_id` | `guid` | Sí | Identificador único de la asignación. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Detalle completo de la asignación operativa. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`).

```json
{
  "assignment_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "operational_order_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "reception_code": "REC-2026-0001",
  "po_code": "PO-2026-0001",
  "is_alerted": false,
  "destination_type": "Warehouse",
  "status": "Pending",
  "merchandise": "Contenedor 40ft",
  "merchandise_description": "Contenedor con mercancía paletizada",
  "has_machinery_assigned": true,
  "has_collaborators_assigned": true,
  "has_outsourced_crew": true,
  "has_outsourced_machinery": true,
  "created_at": "2025-01-15T10:30:00Z",
  "observations": "Asignación para descarga de contenedor",
  "additional_data": "{}",
  "outsourced_crew_count": 4,
  "outsourced_machinery_supplier": "Maquinarias del Norte S.A.",
  "warehouse_information": {
    "warehouse_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "WH-001",
    "warehouse_type": "Warehouse"
  }
}
```

**Campos de `AssignmentOperationalDetailsDto` (hereda de `AssignmentOperationalDto`):**

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
| `observations` | `string` | Observaciones adicionales. |
| `additional_data` | `string` | Datos adicionales (JSON serializado). |
| `outsourced_crew_count` | `int?` | Cantidad de personal externo. Puede ser `null`. |
| `outsourced_machinery_supplier` | `string` | Proveedor de maquinaria externa. Puede ser `null`. |
| `warehouse_information` | `object` | Información del almacén (si aplica). |
| `warehouse_information.warehouse_id` | `guid` | Identificador del almacén. |
| `warehouse_information.code` | `string` | Código del almacén. |
| `warehouse_information.warehouse_type` | `enum (WarehouseType)` | Tipo de almacén. |

### 400 Bad Request

Error de validación (parámetros inválidos).

### 404 Not Found

Asignación no encontrada.

### 500 Internal Server Error

Error no controlado del servidor.