# Solicitudes de Combustible

## Obtener Detalle de Solicitud de Combustible

Endpoint para obtener el detalle completo de una solicitud de combustible.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/fuel-requests/{fuel_request_id}/detail` |
| **Descripción** | Retorna el detalle completo de la solicitud de combustible incluyendo maquinaria, producto/stock, información de área, centro de costos, sucursal y auditoría. Requiere autenticación con token. |

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
    "branch_code": "ALP-MGA"
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
}
```

**Campos de `GetFuelRequestDetailDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `fuel_request_id` | `guid` | Identificador único de la solicitud de combustible. |
| `fuel_request_code` | `string` | Código de la solicitud de combustible. |
| `outflow_status` | `enum (OutflowStatus)` | Estado de la solicitud. |
| `machinery_code` | `string` | Código de la maquinaria o vehículo. |
| `machinery_brand` | `string` | Marca de la maquinaria. |
| `machinery_model` | `string` | Modelo de la maquinaria. |
| `requested_by_user_id` | `guid` | Identificador del usuario solicitante. |
| `requested_by_user_name` | `string` | Nombre del usuario solicitante. |
| `created_at` | `datetime` | Fecha de creación de la solicitud. |
| `work_area_information` | `WorkAreaInformation` | Información del área de trabajo. |
| `cost_center_information` | `CostCenterInformation` | Información del centro de costos. |
| `branch_information` | `BranchInformation` | Información de la sucursal. |
| `machinery_information` | `MachineryInformation` | Información detallada de la maquinaria. |
| `product_stock_id` | `guid` | Identificador del stock de combustible. |
| `product_stock_name` | `string` | Nombre del producto combustible. |
| `requested_quantity` | `decimal` | Cantidad de combustible solicitada. |
| `mileage` | `decimal \| null` | Kilometraje del vehículo. |
| `engine_hours` | `decimal \| null` | Horas motor de la maquinaria. |
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
| `MachineryInformation` | `machinery_id` | `guid` | Identificador de la maquinaria. |
| `MachineryInformation` | `code` | `string` | Código de la maquinaria. |
| `MachineryInformation` | `brand` | `string` | Marca de la maquinaria. |
| `MachineryInformation` | `year` | `string` | Año de la maquinaria. |
| `MachineryInformation` | `model` | `string` | Modelo de la maquinaria. |
| `MachineryInformation` | `serial_number` | `string` | Número de serie. |
| `MachineryInformation` | `color` | `string` | Color. |
| `MachineryInformation` | `status` | `enum (MachineryStatus)` | Estado de la maquinaria. |
| `MachineryInformation` | `type` | `enum (MachineryType)` | Tipo de maquinaria. |

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