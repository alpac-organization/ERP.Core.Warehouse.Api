# Solicitudes Operativas de Inventario

## Obtener Detalle de Solicitud de Inventario

Endpoint para obtener el detalle completo de una solicitud operativa de inventario.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/supply-requests/{supply_request_id}/detail` |
| **Descripción** | Retorna el detalle completo de la solicitud operativa de inventario incluyendo información del área de trabajo, centro de costos, sucursal, almacén, orden operativa e ítems solicitados. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `supply_request_id` | `guid` | Sí | Identificador único de la solicitud de inventario. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Detalle completo de la solicitud operativa de inventario.

```json
{
  "supply_request_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "supply_request_code": "SI-00001",
  "reason_use": "Maintenance",
  "justification": "Repuesto necesario para mantenimiento",
  "outflow_status": "Pending",
  "requested_by_user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "requested_by_user_name": "Juan Pérez",
  "items_count": 3,
  "work_area_information": {
    "work_area_id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
    "work_area_name": "Mantenimiento"
  },
  "cost_center_information": {
    "cost_center_id": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
    "cost_center_name": "Centro de Costos General"
  },
  "branch_information": {
    "branch_id": "3fa85f64-5717-4562-b3fc-2c963f66afb0",
    "branch_code": "ALP-MGA"
  },
  "warehouse_information": {
    "warehouse_id": "3fa85f64-5717-4562-b3fc-2c963f66afb1",
    "warehouse_code": "ALM-01"
  },
  "operational_order_information": {
    "operational_order_id": "3fa85f64-5717-4562-b3fc-2c963f66afb2",
    "operational_order_code": "OO-00001"
  },
  "items": [
    {
      "supply_request_item_id": "3fa85f64-5717-4562-b3fc-2c963f66afb3",
      "product_stock_id": "3fa85f64-5717-4562-b3fc-2c963f66afb4",
      "product_stock_name": "Filtro de Aire",
      "amount": 2
    }
  ]
}
```

**Campos de `GetSupplyRequestDetailDto`:**

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
| `work_area_information` | `WorkAreaInformation` | Información del área de trabajo. |
| `cost_center_information` | `CostCenterInformation` | Información del centro de costos. |
| `branch_information` | `BranchInformation` | Información de la sucursal. |
| `warehouse_information` | `WarehouseInformation` | Información del almacén. |
| `operational_order_information` | `OperationalOrderInformation` | Información de la orden operativa. |
| `items` | `List<ItemDetail>` | Listado de ítems solicitados. |

**Campos de objetos relacionados:**

| Objeto | Campo | Tipo | Descripción |
|--------|-------|------|-------------|
| `WorkAreaInformation` | `work_area_id` | `guid` | Identificador del área de trabajo. |
| `WorkAreaInformation` | `work_area_name` | `string` | Nombre del área de trabajo. |
| `CostCenterInformation` | `cost_center_id` | `guid` | Identificador del centro de costos. |
| `CostCenterInformation` | `cost_center_name` | `string` | Nombre del centro de costos. |
| `BranchInformation` | `branch_id` | `guid` | Identificador de la sucursal. |
| `BranchInformation` | `branch_code` | `string` | Código de la sucursal. |
| `WarehouseInformation` | `warehouse_id` | `guid` | Identificador del almacén. |
| `WarehouseInformation` | `warehouse_code` | `string` | Código del almacén. |
| `OperationalOrderInformation` | `operational_order_id` | `guid` | Identificador de la orden operativa. |
| `OperationalOrderInformation` | `operational_order_code` | `string` | Código de la orden operativa. |
| `ItemDetail` | `supply_request_item_id` | `guid` | Identificador del ítem de la solicitud. |
| `ItemDetail` | `product_stock_id` | `guid` | Identificador del producto en stock. |
| `ItemDetail` | `product_stock_name` | `string` | Nombre del producto en stock. |
| `ItemDetail` | `amount` | `int` | Cantidad solicitada. |

### 400 Bad Request

Error de validación (parámetros inválidos).

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
