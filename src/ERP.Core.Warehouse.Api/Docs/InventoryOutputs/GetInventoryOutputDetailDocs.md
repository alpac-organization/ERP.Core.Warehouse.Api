# Salidas de Inventario

## Obtener Detalle de Salida de Inventario

Endpoint para obtener el detalle completo de una salida de inventario. La salida puede estar asociada a una solicitud de insumos o a una solicitud de combustible.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/inventory-outputs/{inventory_output_id}/detail` |
| **Descripción** | Retorna el detalle de una salida de inventario incluyendo la solicitud asociada (supply o fuel), el resumen de stock y datos de despacho. Requiere autenticación con token. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `inventory_output_id` | `guid` | Sí | Identificador único de la salida de inventario. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Detalle de la salida de inventario.

#### Escenario 1: salida asociada a solicitud de insumos (`request_type = Supply`)

```json
{
  "supply_request_detail": {
    "work_area_information": {
      "work_area_id": "ac234979-34c5-4564-930e-05cf3ba2c1ac",
      "work_area_code": 4,
      "description": "ALMACEN",
      "work_area_name": "ALMACEN",
      "cost_centers": []
    },
    "cost_center_information": {
      "cost_center_id": "d7d8fb51-918a-4f4e-99f7-f702fd7c5334",
      "description": null,
      "cost_center_name": "ALMACÉN MANAGUA",
      "coil_code": -1,
      "cost_center_code": "02"
    },
    "branch_information": {
      "branch_id": "f9c8c488-f53e-46c2-9594-1e9b23cf805c",
      "branch_code": "ALP-MGA",
      "branch_name": "Almacenadora del Pacífico S.A.",
      "company_alias": "ALPAC"
    },
    "warehouse_information": {
      "warehouse_id": "dfa28bea-daf0-43f7-bdf8-3343f7a01acc",
      "code": "BODEGA-02",
      "warehouse_type": "Fiscal"
    },
    "operational_order_information": {
      "po_code": "PO-02",
      "packages_count": 42.00,
      "operational_order_id": "6b428372-50fc-4192-a5ed-1b9012d797c0",
      "is_alerted": true
    },
    "items": [
      {
        "supply_request_item_id": "a2296e42-5a1e-4b35-b286-67a42ac34a9a",
        "product_stock_id": "2be8dc6a-8dac-48c7-9b66-5ce7a27dd710",
        "product_stock_name": "Aceite de motor 15W40",
        "amount": 100.00
      }
    ],
    "approved_by_user_chief_name": "Administrador",
    "annulled_by_user_chief_name": null,
    "supply_request_id": "65c77304-affb-4455-9f49-8151ad2c2bb9",
    "supply_request_code": "OR-ALP-MGA-018",
    "po_code": null,
    "reason_use": "Operation",
    "justification": "dadasd",
    "outflow_status": "Approved",
    "requested_by_user_id": "7293b6b6-3070-402d-8594-34321cfabf07",
    "requested_by_user_name": "Administrador",
    "work_area_name": null,
    "warehouse_code": "BODEGA-02",
    "created_at": "2026-11-06T14:27:56.578682Z",
    "items_count": 0
  },
  "fuel_request_detail": null,
  "inventory_stock_informations": [
    {
      "inventory_stock_id": "2be8dc6a-8dac-48c7-9b66-5ce7a27dd710",
      "product_id": "f36e64b3-0d7f-4a5a-a4c2-65eb083173f5",
      "available_quantity": 2000,
      "weighted_average_cost": 0.00,
      "status": "Stable",
      "unit_measure_id": "cffb6eb0-3239-4fa0-9d11-cb31d1bdbde5"
    }
  ],
  "dispatched_by_user_name": null,
  "dispatched_at": null,
  "dispatched_justification": null
}
```

#### Escenario 2: salida asociada a solicitud de combustible (`request_type = Fuel`)

```json
{
  "supply_request_detail": null,
  "fuel_request_detail": {
    "fuel_request_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fuel_request_code": "FR-00001",
    "outflow_status": "PendingReview",
    "requested_by_user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    "requested_by_user_name": "Juan Pérez",
    "machinery_code": "MACH-001",
    "machinery_brand": "Caterpillar",
    "machinery_model": "D6",
    "created_at": "2026-11-05 08:00:00",
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
      "branch_code": "ALP-MGA",
      "branch_name": "Almacenadora del Pacífico S.A.",
      "company_alias": "ALPAC"
    },
    "machinery_information": {
      "machinery_id": "3fa85f64-5717-4562-b3fc-2c963f66afb1",
      "code": "MACH-001",
      "brand": "Caterpillar",
      "year": "2024",
      "model": "D6",
      "serial_number": "SN-123",
      "color": "Amarillo",
      "status": "Available",
      "type": "HeavyEquipment"
    },
    "product_stock_id": "3fa85f64-5717-4562-b3fc-2c963f66afb2",
    "product_stock_name": "Diesel",
    "requested_quantity": 25,
    "mileage": 12345.5,
    "engine_hours": 850,
    "approved_by_user_chief_name": null,
    "annulled_by_user_chief_name": null
  },
  "inventory_stock_informations": [
    {
      "inventory_stock_id": "3fa85f64-5717-4562-b3fc-2c963f66afb2",
      "product_id": "f36e64b3-0d7f-4a5a-a4c2-65eb083173f5",
      "available_quantity": 2000,
      "weighted_average_cost": 0.00,
      "status": "Stable",
      "unit_measure_id": "cffb6eb0-3239-4fa0-9d11-cb31d1bdbde5"
    }
  ],
  "dispatched_by_user_name": null,
  "dispatched_at": null,
  "dispatched_justification": null
}
```

**Campos de `GetInventoryOutputDetailDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `supply_request_detail` | `GetSupplyRequestDetailDto \| null` | Detalle completo de la solicitud de insumos asociada. |
| `fuel_request_detail` | `GetFuelRequestDetailDto \| null` | Detalle completo de la solicitud de combustible asociada. |
| `inventory_stock_informations` | `List<InventoryStockInformation>` | Resumen de stock de cada producto relacionado. |
| `dispatched_by_user_name` | `string \| null` | Nombre del usuario que despachó la salida (auditoría). |
| `dispatched_at` | `datetime \| null` | Fecha/hora de despacho. |
| `dispatched_justification` | `string \| null` | Justificación del despacho. |

**Campos de `InventoryStockInformation`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `inventory_stock_id` | `guid` | Identificador del stock de producto. |
| `product_id` | `guid` | Identificador del producto. |
| `available_quantity` | `int` | Cantidad disponible actualmente. |
| `weighted_average_cost` | `decimal` | Costo promedio ponderado. |
| `status` | `enum (ProductStockStatus)` | Estado del stock. |
| `unit_measure_id` | `guid` | Identificador de la unidad de medida. |

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

### 404 Not Found

La salida de inventario no existe.

### 500 Internal Server Error

Error no controlado del servidor.