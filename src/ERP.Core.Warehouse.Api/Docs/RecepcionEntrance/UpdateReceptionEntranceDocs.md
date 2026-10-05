# Control de Acceso

## Actualizar Registro de Recepción

Endpoint para actualizar de forma parcial un registro de entrada de recepción existente.

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` |
| **Descripción** | Actualiza parcialmente la información general y/o de transporte de un registro de recepción. Permite agregar nuevas evidencias fotográficas y eliminar evidencias existentes. **No crea ni elimina órdenes operativas (PO) asociadas**, pero sí sincroniza el número de documento cuando este se renombra. |

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
| `document_type`               | `integer (enum DocumentType)` | No      | Tipo de documento: `3 = DUCA`, `4 = CustomsDeclaration`. Opcional: si se omite, se infiere del campo de números enviado, y si ambos se omiten se usa el tipo actual de la recepción. Se envía como **número**. |
| `ducat_numbers`               | `array<string>`            | No        | Lista **completa** de números DUCA. Ver [Sincronización de documentos](#sincronización-de-documentos). |
| `customs_declaration_number`  | `string`                   | No        | Número de declaración aduanera. Ver [Sincronización de documentos](#sincronización-de-documentos). |

### ReceptionTransportInformation

| Parámetro                  | Tipo                       | Requerido | Descripción |
|----------------------------|----------------------------|-----------|-------------|
| `driver_name`              | `string`                   | No        | Nombre del conductor. |
| `driver_license`           | `string`                   | No        | Licencia de conducir. |
| `transportista`            | `string`                   | No        | Nombre del transportista. |
| `vehicle_plate_number`     | `string`                   | No        | Placa del vehículo. |
| `vehicle_chassis_number`   | `string`                   | No        | Número de chasis del vehículo. |
| `transport_unit`           | `integer (enum TransportUnit)` | No     | Tipo de unidad de transporte. Se envía como **número**. Si se omite, conserva el valor actual. |

---

## Sincronización de documentos

La lista de números de documento se envía **completa**, igual que en el POST. El backend calcula la diferencia contra los valores actuales y aplica únicamente los cambios necesarios.

### Cómo se determina qué documento cambió

1. Se obtiene el tipo efectivo del documento: `document_type` enviado → si no, se infiere de `ducat_numbers` (DUCA) o `customs_declaration_number` (CustomsDeclaration) → si no, el tipo actual de la recepción.
2. Se compara la lista enviada contra la lista actual y se obtienen los valores **salientes** (`removed`) y **entrantes** (`added`).
3. Si hay exactamente un valor saliente y uno entrante, se renombra ese documento. El `document_id` del JSON y el `id` de la orden operativa asociada se **preservan**, de modo que cualquier asignación asociada sigue apuntando a la misma orden.

### Cascada a órdenes operativas

Cuando un número se renombra, el cambio se propaga a `operational_orders.document_number` de la orden que coincida con el valor anterior. La orden se localiza por igualdad exacta de `document_number`, no por posición.

### Restricciones

| Regla | Código de error |
|-------|-----------------|
| No se puede cambiar la cantidad de documentos. | `ERP:DOCUMENT_COUNT_MISMATCH` |
| Solo se puede renombrar **un** documento por operación. Para cambiar varios, enviar varios PATCH. | `ERP:MULTIPLE_DOCUMENT_RENAMES` |
| La lista no puede contener números duplicados. | `ERP:DUPLICATE_DOCUMENT_NUMBERS` |
| No se puede enviar `ducat_numbers` y `customs_declaration_number` en la misma petición. | `ERP:CONFLICTING_DOCUMENT_FIELDS` |
| El tipo de documento solo puede cambiarse cuando la recepción tiene **un solo** documento. | `ERP:DOCUMENT_TYPE_CHANGE_NOT_ALLOWED` |
| `document_type` distinto de `DUCA` y `CustomsDeclaration`. | `ERP:INVALID_DOCUMENT_TYPE` |

> **Nota:** Si la recepción tiene varios documentos, cambiar `document_type` está bloqueado porque la cantidad de órdenes operativas depende del tipo (una por DUCA, una sola para CustomsDeclaration). Además, cambiar el tipo a `CustomsDeclaration` no reconstruye la `AssignmentOperational` que la creación habría generado.

---

## Ejemplos de sincronización

**Renombrar un solo DUCA.** La recepción tiene `["DUCA-A", "DUCA-B", "DUCA-C"]`. Se envía la lista completa con un solo valor distinto:

```json
{
  "general_information": {
    "document_type": 3,
    "ducat_numbers": ["DUCA-A", "DUCA-B", "DUCA-X"]
  }
}
```

Resultado: `DUCA-C` pasa a ser `DUCA-X` tanto en `additional_data` como en `operational_orders.document_number`. `DUCA-A` y `DUCA-B` no se modifican.

**Actualizar un solo campo sin tocar documentos.** No se envía `document_type` ni números, por lo que no se realiza ninguna sincronización:

```json
{
  "general_information": {
    "seal_number": "468468dewd"
  }
}
```

---

---

## Reglas de Negocio

| Regla | Descripción |
|-------|-------------|
| PATCH parcial | Solo se sobrescriben las propiedades enviadas con valor (no null). Las propiedades omitidas conservan su valor actual. Se pueden enviar **varios campos en la misma petición**. |
| Varios campos | No hay restricción de cantidad: el body acepta cualquier combinación de `general_information`, `reception_transport_information`, `evidence_base64` y `evidence_ids_to_delete`. |
| Evidencias nuevas | Las imágenes en `evidence_base64` se suben a S3 y se anexan a la lista existente en `AdditionalData`. |
| Eliminar evidencias | Los IDs en `evidence_ids_to_delete` se eliminan de la lista de evidencias en `AdditionalData`. |
| Números de documento | El renombre se propaga a `operational_orders.document_number` conservando el identificador de la orden. Ver [Sincronización de documentos](#sincronización-de-documentos). |
| Tipo de documento | Solo puede cambiarse cuando la recepción tiene un único documento. No se crean ni eliminan órdenes operativas. |
| Rol Operator | Los usuarios con rol `Operator` reciben error `400`: `No tienes acceso a realizar esta acción` (`ERP:INVALID_ACCESS`). |
| Rol ausente | Si el rol no puede determinarse, la operación se rechaza con `ERP:INVALID_ACCESS`. |
| Ventana de 10 minutos | Pasados 10 minutos desde la creación, solo `Administrator` y `Manager` pueden actualizar (`ERP:RECEPTION_UPDATE_TIME_EXPIRED`). |

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
| Rol `Operator` o rol ausente | Recibe 400: `No tienes acceso a realizar esta acción` (`ERP:INVALID_ACCESS`). |
| Fuera de la ventana de 10 minutos | Recibe 400: `Ya no se puede modificar la información vehicular de la recepción` (`ERP:RECEPTION_UPDATE_TIME_EXPIRED`). |
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
| `ERP:INVALID_ACCESS` | El usuario no tiene acceso a la compañía/módulo, tiene rol `Operator`, o su rol no pudo determinarse. |
| `ERP:RECEPTION_UPDATE_TIME_EXPIRED` | Se intentó actualizar pasado el plazo de 10 minutos y el rol no es `Administrator` ni `Manager`. |
| `ERP:CONFLICTING_DOCUMENT_FIELDS` | Se enviaron `ducat_numbers` y `customs_declaration_number` juntos. |
| `ERP:INVALID_DOCUMENT_TYPE` | `document_type` distinto de `DUCA` (3) y `CustomsDeclaration` (4). |
| `ERP:DUPLICATE_DOCUMENT_NUMBERS` | La lista de números contiene valores duplicados. |
| `ERP:DOCUMENT_TYPE_CHANGE_NOT_ALLOWED` | Se intentó cambiar `document_type` en una recepción con más de un documento. |
| `ERP:DOCUMENT_COUNT_MISMATCH` | La cantidad de documentos enviados difiere de la cantidad de órdenes operativas de la recepción. |
| `ERP:MULTIPLE_DOCUMENT_RENAMES` | Se intentó renombrar más de un documento en una sola petición. |
| `ERP:DOCUMENT_SYNC_INCONSISTENT` | La información de documentos quedó inconsistente y no se aplicó ningún cambio. |
| `ERP:ERROR_CUSTOM_BRANCH` | La `custom_branch_id` enviada no corresponde a una aduana activa. |
| `ERP:ERROR_UPDATED` | No se encontró la información de transporte asociada a la recepción. |

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
    "document_type": 3,
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