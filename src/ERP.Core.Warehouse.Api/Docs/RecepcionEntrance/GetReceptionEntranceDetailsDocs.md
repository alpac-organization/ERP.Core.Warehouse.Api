# Control de Acceso

## Obtener Detalle de Registro de Recepción

Endpoint para obtener el detalle completo de un registro de entrada de recepción.

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details` |
| **Descripción** | Retorna el detalle completo de un registro de recepción incluyendo información general, información de transporte, aduana/ramal y datos adicionales (evidencias, documentos). |

---

## Parámetros de Ruta (Path Params)

| Parámetro                 | Tipo     | Requerido | Descripción |
|:-------------------------:|:--------:|-----------|-------------|
| `company_id`              | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`             | `string` | Sí        | Código del módulo dentro de la compañía. |
| `reception_entrance_id`   | `guid`   | Sí        | Identificador del registro de recepción a consultar. Debe existir. |

> **Nota:** El parámetro `reception_id` en la ruta del controlador no se utiliza en la query actual.

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Respuestas

### ✅ 200 OK

Retorna el detalle completo del registro de recepción.

**Estructura de respuesta (`ReceptionEntranceDetailsDto`):**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `reception_code` | `string \| null` | Código generado automáticamente para la recepción. |
| `reception_entrance_id` | `guid` | Identificador único del registro de recepción. |
| `seal_number` | `string` | Número de precinto/sello. |
| `container_number` | `string` | Número de contenedor. |
| `country_of_origin` | `string` | País de origen. |
| `created_at` | `datetime` | Fecha y hora de creación del registro (formato ISO 8601). |
| `additional_data` | `string \| null` | Datos adicionales serializados en JSON (snake_case). Incluye URLs de evidencias y información de documentos. |
| `custom_branches_information` | `CustomBranchesInformation` | Información de la aduana/ramal asociada. |
| `reception_transport_entrance_information` | `ReceptionTransportEntranceDto` | Información completa del transporte. |

#### CustomBranchesInformation

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `custom_branch_id` | `guid` | Identificador de la aduana/ramal. |
| `custom_branch_code` | `string` | Código de la aduana/ramal. |
| `custom_branch_name` | `string` | Nombre de la aduana/ramal. |

#### ReceptionTransportEntranceDto

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `driver_name` | `string` | Nombre del conductor. |
| `driver_license` | `string` | Licencia de conducir. |
| `transportista` | `string` | Nombre del transportista. |
| `vehicle_plate_number` | `string` | Placa del vehículo. |
| `vehicle_chassis_number` | `string` | Número de chasis del vehículo. |
| `transport_unit` | `string (enum TransportUnit)` | Tipo de unidad de transporte: `Truck`, `Container`, `Rail`, `Ship`, `Plane`, etc. |

#### AdditionalData (JSON deserializado)

El campo `additional_data` contiene un JSON con la siguiente estructura:

```json
{
  "evidence_urls": [
    {
      "document_id": "guid",
      "document_url": "https://bucket.s3.region.amazonaws.com/Warehouse/ReceptionEntrance/guid.jpg"
    }
  ],
  "document_numbers": [
    {
      "document_id": "guid",
      "document_numbers": "DUCA-2026-001",
      "document_type": "DUCA"
    },
    {
      "document_id": "guid",
      "document_numbers": "CUST-2026-0001",
      "document_type": "CustomsDeclaration"
    }
  ]
}
```

### Ejemplo de Respuesta (200 OK)

```json
{
  "reception_code": "REC-20260930-001",
  "reception_entrance_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000001",
  "seal_number": "SEAL-12345",
  "container_number": "CONT-987654",
  "country_of_origin": "China",
  "created_at": "2026-09-30T10:30:00Z",
  "additional_data": "{\"evidence_urls\":[{\"document_id\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"document_url\":\"https://bucket.s3.region.amazonaws.com/Warehouse/ReceptionEntrance/image1.jpg\"}],\"document_numbers\":[{\"document_id\":\"11111111-1111-1111-1111-111111111111\",\"document_numbers\":\"DUCA-2026-001\",\"document_type\":\"DUCA\"}]}",
  "custom_branches_information": {
    "custom_branch_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "custom_branch_code": "CUST-001",
    "custom_branch_name": "Aduana Principal Managua"
  },
  "reception_transport_entrance_information": {
    "driver_name": "Juan Pérez",
    "driver_license": "LIC-123456",
    "transportista": "Transportes Rápidos S.A.",
    "vehicle_plate_number": "ABC-123",
    "vehicle_chassis_number": "CHASSIS-987654321",
    "transport_unit": "Truck"
  }
}
```

---

### ❌ 400 Bad Request

Error de validación, acceso o reglas de negocio usando `ErrorResponse`:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-30 12:00:00"
}
```

### ❌ 404 Not Found

El registro de recepción no existe:

```json
{
  "status": 404,
  "error": {
    "type_error": "NotFound",
    "description": "No se encontró el registro de recepción"
  },
  "created_at": "2026-09-30 12:00:00"
}
```

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud"
  },
  "created_at": "2026-09-30 12:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Detalle de registro de recepción obtenido exitosamente. |
| `400` | Error de validación, acceso o permisos (`ErrorResponse`). |
| `404` | Registro de recepción no encontrado (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |