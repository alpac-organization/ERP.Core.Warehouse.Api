# Control de Acceso

## Actualizar Registro de Recepción

Endpoint para actualizar de forma parcial un registro de entrada de recepción existente.

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` |
| **Descripción** | Actualiza parcialmente la información general y/o de transporte de un registro de recepción. Permite agregar nuevas evidencias fotográficas y eliminar evidencias existentes. **No modifica el tipo de documento ni crea/elimina órdenes operativas (PO) asociadas.** |

---

## Parámetros de Ruta (Path Params)

| Parámetro                 | Tipo     | Requerido | Descripción |
|:-------------------------:|:--------:|-----------|-------------|
| `company_id`              | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`             | `string` | Sí        | Código del módulo dentro de la compañía. |
| `reception_entrance_id`   | `guid`   | Sí        | Identificador del registro de recepción a actualizar. Debe existir. |

> **Nota:** El parámetro `reception_id` en la ruta del controlador no se utiliza en el comando actual.

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code` y `reception_entrance_id` no forman parte del JSON: se toman del token y de la ruta (`[JsonIgnore]`).

Todas las propiedades del body son **opcionales** (PATCH parcial). Solo se actualiza lo que se envía con valor.

| Parámetro                      | Tipo                                      | Requerido | Descripción |
|--------------------------------|-------------------------------------------|-----------|-------------|
| `general_information`          | `object (GeneralInformationUpdated)`      | No        | Información general a actualizar. |
| `reception_transport_information` | `object (ReceptionTransportInformation)` | No        | Información de transporte a actualizar. |
| `evidence_base64`              | `array<string>`                           | No        | Nuevas imágenes en Base64 para agregar como evidencia. Se suben a S3 y se anexan a las existentes. |
| `evidence_ids_to_delete`       | `array<guid>`                             | No        | Lista de IDs de evidencias existentes a eliminar. |

### GeneralInformationUpdated

| Parámetro                     | Tipo                       | Requerido | Descripción |
|-------------------------------|----------------------------|-----------|-------------|
| `custom_branch_id`            | `guid`                     | No        | Identificador de la aduana/ramal. |
| `seal_number`                 | `string`                   | No        | Número de precinto/sello. |
| `country_origin`              | `string`                   | No        | País de origen. |
| `container_number`            | `string`                   | No        | Número de contenedor. |
| `document_type`               | `integer (enum DocumentType)` | No      | Tipo de documento: `1 = DUCA`, `2 = CustomsDeclaration`. Se envía como **número**. |
| `ducat_numbers`               | `array<string>`            | No        | Lista de números DUCA. |
| `customs_declaration_number`  | `string`                   | No        | Número de declaración aduanera. |

### ReceptionTransportInformation

| Parámetro                  | Tipo                       | Requerido | Descripción |
|----------------------------|----------------------------|-----------|-------------|
| `driver_name`              | `string`                   | No        | Nombre del conductor. |
| `driver_license`           | `string`                   | No        | Licencia de conducir. |
| `transportista`            | `string`                   | No        | Nombre del transportista. |
| `vehicle_plate_number`     | `string`                   | No        | Placa del vehículo. |
| `vehicle_chassis_number`   | `string`                   | No        | Número de chasis del vehículo. |
| `transport_unit`           | `integer (enum TransportUnit)` | No     | Tipo de unidad de transporte. Se envía como **número**. |

---

## Reglas de Negocio

| Regla | Descripción |
|-------|-------------|
| PATCH parcial | Solo se sobrescriben las propiedades enviadas con valor (no null). Las propiedades omitidas conservan su valor actual. |
| Evidencias nuevas | Las imágenes en `evidence_base64` se suben a S3 y se anexan a la lista existente en `AdditionalData`. |
| Eliminar evidencias | Los IDs en `evidence_ids_to_delete` se eliminan de la lista de evidencias en `AdditionalData`. |
| Tipo de documento | Se puede cambiar el `document_type`, pero **no se regeneran ni modifican las órdenes operativas (PO) ya creadas**. |
| Rol Supervisor | Los usuarios con rol `Supervisor` reciben error `400`: `No tienes acceso a realizar esta acción` (`ERP:INVALID_ACCESS`). |
| Validación condicional | Si se cambia a `document_type = DUCA`, se debe enviar al menos un `ducat_number`. Si se cambia a `CustomsDeclaration`, se requiere `customs_declaration_number` (validación no implementada en validator actual, pero regla de negocio del handler de creación). |

---

## Respuestas

### ✅ 200 OK

El registro se actualizó correctamente. El cuerpo de la respuesta puede ir vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| PATCH parcial | Solo se sobrescriben propiedades enviadas con valor (`!= null`). |
| `evidence_base64` vacío / ausente | No se agregan nuevas evidencias. |
| `evidence_ids_to_delete` vacío / ausente | No se eliminan evidencias. |
| Rol `Supervisor` | Recibe 400: `No tienes permiso para realizar esta acción` (`ERP:INVALID_ACCESS`). |
| Registro no encontrado | Recibe error desde el handler cuando no existe. |
| Enums (`document_type`, `transport_unit`) | Se envían como **enteros**. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` en errores de validación FluentValidation, acceso y reglas de negocio:

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

**Casos comunes de error 400:**

| Código de error | Descripción |
|-----------------|-------------|
| `ValidationError` | Validación de FluentValidation (aunque el validator actual está vacío, pueden venir errores de model binding). |
| `ERP:INVALID_ACCESS` | El usuario no tiene acceso a la compañía/módulo o tiene rol Supervisor. |

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

---

## Ejemplo de Request

```json
{
  "general_information": {
    "custom_branch_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
    "seal_number": "SEAL-99999",
    "country_origin": "México",
    "container_number": "CONT-NEW-001",
    "document_type": 1,
    "ducat_numbers": ["DUCA-2026-003"],
    "customs_declaration_number": null
  },
  "reception_transport_information": {
    "driver_name": "Pedro García",
    "driver_license": "LIC-NEW-789",
    "transportista": "Nueva Logística S.A.",
    "vehicle_plate_number": "NEW-456",
    "vehicle_chassis_number": "CHASSIS-NEW-111",
    "transport_unit": 1
  },
  "evidence_base64": ["base64_string_new_image"],
  "evidence_ids_to_delete": ["aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"]
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Registro de recepción actualizado exitosamente. |
| `400` | Error de validación, acceso, permisos, reglas de negocio (`ErrorResponse`) o fallo de deserialización del body (model binding). |
| `500` | Error interno del servidor (`ErrorResponse`). |