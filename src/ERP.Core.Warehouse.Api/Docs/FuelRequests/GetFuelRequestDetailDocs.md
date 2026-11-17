# Solicitudes de Combustible

## Obtener Detalle de Solicitud de Combustible

Endpoint para obtener el detalle completo de una solicitud de combustible.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/fuel-requests/{fuel_request_id}/detail` |
| **Descripción** | Retorna el detalle completo de la solicitud de combustible incluyendo equipo, producto/stock, información de área, centro de costos, sucursal, almacén y auditoría. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `fuel_request_id` | `guid` | Sí | Identificador único de la solicitud de combustible. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Detalle completo de la solicitud de combustible.

```json
{
  "fuel_request_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fuel_request_code": "FR-00001",
  "outflow_status": "PendingReview",
  "requested_by_user_id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "requested_by_user_name": "Juan Pérez",
  "equipment_code": "EQ-001",
  "equipment_brand": "Toyota",
  "equipment_model": "7FDU35",
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
    "branch_code": "ALP-MGA"
  },
  "equipment_information": {
    "brand": "Toyota",
    "code": "EQ-001",
    "model": "7FDU35",
    "serial_number": "SN-123",
    "status": "Available",
    "type": "Forklift",
    "gasoline": false,
    "diesel": true,
    "electric": false,
    "lpg": false
  },
  "warehouse_information": {
    "warehouse_id": "3fa85f64-5717-4562-b3fc-2c963f66afb5"
  },
  "product_stock_id": "3fa85f64-5717-4562-b3fc-2c963f66afb2",
  "product_stock_name": "Diesel",
  "requested_quantity": 25,
  "mileage": 12345.5,
  "engine_hours": 850,
  "previous_mileage": 12000.0,
  "previous_engine_hours": 820,
  "approved_by_user_chief_name": null,
  "annulled_by_user_chief_name": null
}
```

**Campos de `GetFuelRequestDetailDto`:**

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
| `work_area_information` | `WorkAreaInformation` | Información del área de trabajo. |
| `cost_center_information` | `CostCenterInformation` | Información del centro de costos. |
| `branch_information` | `BranchInformation` | Información de la sucursal. |
| `equipment_information` | `EquipmentInformation` | Información detallada del equipo. |
| `warehouse_information` | `WarehouseInformation` | Información del almacén. |
| `product_stock_id` | `guid` | Identificador del stock de combustible. |
| `product_stock_name` | `string` | Nombre del producto combustible. |
| `requested_quantity` | `decimal` | Cantidad de combustible solicitada. |
| `mileage` | `decimal \| null` | Kilometraje del vehículo. |
| `engine_hours` | `decimal \| null` | Horas motor de la maquinaria. |
| `previous_mileage` | `decimal \| null` | Kilometraje de la solicitud anterior del mismo equipo. |
| `previous_engine_hours` | `decimal \| null` | Horas motor de la solicitud anterior del mismo equipo. |
| `approved_by_user_chief_name` | `string \| null` | Nombre del jefe que aprobó la solicitud. |
| `annulled_by_user_chief_name` | `string \| null` | Nombre del jefe que anuló la solicitud. |

**Campos de objetos relacionados:**

| Objeto | Campo | Tipo | Descripción |
|--------|-------|------|-------------|
| `WorkAreaInformation` | `work_area_id` | `guid` | Identificador del área de trabajo. |
| `WorkAreaInformation` | `work_area_name` | `string` | Nombre del área de trabajo. |
| `CostCenterInformation` | `cost_center_id` | `guid` | Identificador del centro de costos. |
| `CostCenterInformation` | `cost_center_name` | `string` | Nombre del centro de costos. |
| `BranchInformation` | `branch_id` | `guid` | Identificador de la sucursal. |
| `BranchInformation` | `branch_code` | `string` | Código de la sucursal. |
| `EquipmentInformation` | `brand` | `string` | Marca del equipo. |
| `EquipmentInformation` | `code` | `string` | Código del equipo. |
| `EquipmentInformation` | `model` | `string` | Modelo del equipo. |
| `EquipmentInformation` | `serial_number` | `string` | Número de serie. |
| `EquipmentInformation` | `status` | `enum (EquipmentStatus)` | Estado del equipo. |
| `EquipmentInformation` | `type` | `enum (EquipmentType)` | Tipo de equipo. |
| `EquipmentInformation` | `gasoline` | `bool` | Combustible gasolina. |
| `EquipmentInformation` | `diesel` | `bool` | Combustible diésel. |
| `EquipmentInformation` | `electric` | `bool` | Combustible eléctrico. |
| `EquipmentInformation` | `lpg` | `bool` | Combustible GLP. |
| `WarehouseInformation` | `warehouse_id` | `guid` | Identificador del almacén. |

### 400 Bad Request

Error de validación, permisos o acceso.

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

### 404 Not Found

La solicitud de combustible no existe.

### 500 Internal Server Error

Error no controlado del servidor.