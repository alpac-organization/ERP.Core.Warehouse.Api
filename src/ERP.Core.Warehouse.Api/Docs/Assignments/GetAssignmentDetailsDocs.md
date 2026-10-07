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

Detalle completo de la asignación operativa.

```json
{
  "assignmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "operationalOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "isAlerted": false,
  "destinationType": 1,
  "status": 1,
  "merchandise": "Contenedor 40ft",
  "merchandiseDescription": "Contenedor con mercancía paletizada",
  "hasMachineryAssigned": true,
  "hasCollaboratorsAssigned": true,
  "createdAt": "2025-01-15T10:30:00Z",
  "observations": "Asignación para descarga de contenedor",
  "additionalData": "{}",
  "warehouseInformation": {
    "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "code": "WH-001",
    "warehouseType": 1
  }
}
```

**Campos de `AssignmentOperationalDetailsDto` (hereda de `AssignmentOperationalDto`):**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `assignmentId` | `guid` | Identificador único de la asignación. |
| `operationalOrderId` | `guid` | Identificador de la orden operativa padre. |
| `isAlerted` | `bool` | Indica si la asignación tiene alerta. |
| `destinationType` | `enum (DestinationType)` | Tipo de destino: `None` (0), `Warehouse` (1), `CustomYard` (2), `CustomSheld` (3). |
| `status` | `enum (AssignmentOperationalStatus)` | Estado: `None` (0), `Pending` (1), `InProgress` (2), `OnHold` (3), `Downloaded` (4). |
| `merchandise` | `string` | Nombre de la mercancía. |
| `merchandiseDescription` | `string` | Descripción de la mercancía. |
| `hasMachineryAssigned` | `bool` | Indica si tiene maquinaria asignada. |
| `hasCollaboratorsAssigned` | `bool` | Indica si tiene colaboradores asignados. |
| `createdAt` | `datetime` | Fecha de creación (UTC). |
| `observations` | `string` | Observaciones adicionales. |
| `additionalData` | `string` | Datos adicionales (JSON serializado). |
| `warehouseInformation` | `object` | Información del almacén (si aplica). |
| `warehouseInformation.warehouseId` | `guid` | Identificador del almacén. |
| `warehouseInformation.code` | `string` | Código del almacén. |
| `warehouseInformation.warehouseType` | `enum (WarehouseType)` | Tipo de almacén. |

### 400 Bad Request

Error de validación (parámetros inválidos).

### 404 Not Found

Asignación no encontrada.

### 500 Internal Server Error

Error no controlado del servidor.