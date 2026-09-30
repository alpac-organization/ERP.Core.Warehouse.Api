# Control de Acceso

## Crear Registro de Recepción

Endpoint para crear un nuevo registro de entrada de recepción (control de acceso vehicular) con su información de transporte y documentos asociados.

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` |
| **Descripción** | Registra una nueva entrada de recepción incluyendo información general (tipo de documento, números DUCA o declaración aduanera), información de transporte (conductor, vehículo, transportista) y evidencia fotográfica. Adicionalmente crea órdenes operativas (PO) asociadas según el tipo de documento. |

---

## Parámetros de Ruta (Path Params)

| Parámetro     | Tipo     | Requerido | Descripción |
|:-------------:|:--------:|-----------|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo dentro de la compañía. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id` y `module_code` no forman parte del JSON: se toman del token y de la ruta.

Todas las propiedades del body son **requeridas** salvo que se indique lo contrario.

| Parámetro                  | Tipo                                      | Requerido | Descripción |
|----------------------------|-------------------------------------------|-----------|-------------|
| `general_information`      | `object (GeneralInformation)`             | Sí        | Información general de la recepción. |
| `transport_information`    | `object (TransportInformation)`           | Sí        | Información del transporte/vehículo. |
| `customs_declaration_information` | `object (CustomsDeclarationInformation)` | No        | Información de declaración aduanera (requerida si `document_type = CustomsDeclaration`). |
| `evidence_base64`          | `array<string>`                           | No        | Lista de imágenes en Base64 como evidencia. |

### GeneralInformation

| Parámetro                  | Tipo                       | Requerido | Descripción |
|----------------------------|----------------------------|-----------|-------------|
| `custom_branch_id`         | `guid`                     | Sí        | Identificador de la aduana/ramal. |
| `seal_number`              | `string`                   | Sí        | Número de precinto/sello. |
| `country_origin`           | `string`                   | Sí        | País de origen. |
| `container_number`         | `string`                   | Sí        | Número de contenedor. |
| `document_type`            | `integer (enum DocumentType)` | Sí      | Tipo de documento: `1 = DUCA`, `2 = CustomsDeclaration`. Se envía como **número**. |
| `ducat_numbers`            | `array<string>`            | Condicional | Lista de números DUCA. **Requerida si `document_type = DUCA`**. Debe tener al menos un elemento. |
| `customs_declaration_number` | `string`                 | Condicional | Número de declaración aduanera. **Requerido si `document_type = CustomsDeclaration`**. |

### TransportInformation

| Parámetro                  | Tipo                       | Requerido | Descripción |
|----------------------------|----------------------------|-----------|-------------|
| `driver_name`              | `string`                   | Sí        | Nombre del conductor. |
| `driver_license`           | `string`                   | Sí        | Licencia de conducir. |
| `transportista`            | `string`                   | Sí        | Nombre del transportista. |
| `vehicle_plate_number`     | `string`                   | Sí        | Placa del vehículo. |
| `vehicle_chassis_number`   | `string`                   | Sí        | Número de chasis del vehículo. |
| `transport_unit`           | `integer (enum TransportUnit)` | Sí     | Tipo de unidad de transporte. Se envía como **número**. |

### CustomsDeclarationInformation (Solo para `document_type = CustomsDeclaration`)

| Parámetro           | Tipo       | Requerido | Descripción |
|---------------------|------------|-----------|-------------|
| `total_weight`      | `decimal`  | Sí        | Peso total. Debe ser mayor que cero. |
| `package_number`    | `decimal`  | Sí        | Número de bultos. Debe ser mayor que cero. |
| `product_description` | `string` | No        | Descripción del producto. |
| `observations`      | `string`   | No        | Observaciones adicionales. |

---

## Reglas de Negocio

| Regla | Descripción |
|-------|-------------|
| Tipo de documento DUCA | Requiere al menos un número DUCA en `ducat_numbers`. No se debe enviar `customs_declaration_information`. |
| Tipo de documento Declaración Aduanera | Requiere `customs_declaration_information` con `total_weight > 0` y `package_number > 0`. Requiere `customs_declaration_number`. La lista `ducat_numbers` debe estar vacía. |
| Ventana horaria aduanera | Para declaraciones aduaneras, el registro solo está permitido en horarios: **5:00 pm - 8:00 am** (ventana nocturna que cruza medianoche) **y** **12:00 pm - 1:00 pm** (ventana mediodía). Fuera de este horario retorna error `ERP:CUSTOMS_DECLARATION_OUT_OF_WINDOW`. |
| Rol Supervisor | Los usuarios con rol `Supervisor` reciben error `400`: `No tienes acceso a realizar esta acción` (`ERP:INVALID_ACCESS`). |
| Generación de códigos | Se genera automáticamente un código único de recepción (`ReceptionCode`) y códigos de órdenes operativas (PO) para cada DUCA o declaración. |
| Evidencia | Las imágenes en Base64 se suben a S3 y se almacenan sus URLs en `AdditionalData`. |

---

## Respuestas

### ✅ 201 Created

El registro de recepción se creó correctamente. El cuerpo de la respuesta puede ir vacío.

**Headers de respuesta:**
| Header | Valor |
|--------|-------|
| `Location` | URL del recurso creado (puede no estar implementada) |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` en errores de validación FluentValidation, acceso y reglas de negocio:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "Debe indicar al menos un número de DUCA cuando el tipo de documento es DUCA."
  },
  "created_at": "2026-09-30 12:00:00"
}
```

**Casos comunes de error 400:**

| Código de error | Descripción |
|-----------------|-------------|
| `ValidationError` | Validación de FluentValidation (campos requeridos, formatos, reglas condicionales). |
| `ERP:INVALID_ACCESS` | El usuario no tiene acceso a la compañía/módulo o tiene rol Supervisor. |
| `ERP:CUSTOMS_DECLARATION_OUT_OF_WINDOW` | Fuera del horario permitido para declaración aduanera. |
| `ERP:INVALID_DOCUMENT` | Tipo de documento no válido (no es DUCA ni CustomsDeclaration). |

Si un enum se envía con tipo incorrecto (por ejemplo string en lugar de integer), la respuesta puede venir en el formato de model binding de ASP.NET:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "$.document_type": [
      "The JSON value could not be converted to ERP.Core.Database.Domain.Enums.DocumentType."
    ]
  }
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

**Casos comunes de error 500:**

| Código de error | Descripción |
|-----------------|-------------|
| `ERP:CODE_GENERATOR_ERROR` | Error al generar código único de recepción. |
| `ERP:INTERNAL_ERROR` | Error al generar código de orden operativa (PO). |

---

## Ejemplo de Request (DUCA)

```json
{
  "general_information": {
    "custom_branch_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "seal_number": "SEAL-12345",
    "country_origin": "China",
    "container_number": "CONT-987654",
    "document_type": 1,
    "ducat_numbers": ["DUCA-2026-001", "DUCA-2026-002"],
    "customs_declaration_number": null
  },
  "transport_information": {
    "driver_name": "Juan Pérez",
    "driver_license": "LIC-123456",
    "transportista": "Transportes Rápidos S.A.",
    "vehicle_plate_number": "ABC-123",
    "vehicle_chassis_number": "CHASSIS-987654321",
    "transport_unit": 1
  },
  "customs_declaration_information": null,
  "evidence_base64": ["base64_string_image1", "base64_string_image2"]
}
```

## Ejemplo de Request (Declaración Aduanera)

```json
{
  "general_information": {
    "custom_branch_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "seal_number": "SEAL-54321",
    "country_origin": "USA",
    "container_number": "CONT-111222",
    "document_type": 2,
    "ducat_numbers": [],
    "customs_declaration_number": "CUST-2026-0001"
  },
  "transport_information": {
    "driver_name": "Carlos López",
    "driver_license": "LIC-789012",
    "transportista": "Logística Global",
    "vehicle_plate_number": "XYZ-789",
    "vehicle_chassis_number": "CHASSIS-123456789",
    "transport_unit": 2
  },
  "customs_declaration_information": {
    "total_weight": 15000.50,
    "package_number": 250,
    "product_description": "Electrónicos varios",
    "observations": "Carga frágil"
  },
  "evidence_base64": []
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `201` | Registro de recepción creado exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`) o fallo de deserialización del body (model binding). |
| `500` | Error interno del servidor (`ErrorResponse`). |