# Recepciones

## Obtener Detalle de Recepción

Endpoint para obtener el detalle completo de una recepción: información general, documentos asociados, evidencias, aduana de procedencia y datos de transporte.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details` |
| **Descripción** | Devuelve el detalle de la recepción indicada. |
| **Tags**        | `Control de Acceso` |

---

## Parámetros de Ruta (Path Params)

| Parámetro               | Tipo     | Requerido | Descripción |
|:-----------------------:|:--------:|:---------:|-------------|
| `company_id`            | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`           | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `reception_entrance_id` | `guid`   | Sí        | Identificador único de la recepción. Es el valor que devuelve `reception_entrance_id` en el listado. |

> La acción del controller declara además un parámetro `reception_id` que **no forma parte de la plantilla de la ruta** y **nunca se usa**: el id real viaja en `reception_entrance_id`.

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `X-Api-Key`     | `{api_key}`      | Sí        |

---

## Flujo del Handler (`GetReceptionEntranceDetailsHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Busca la recepción con `Include(User)`, `Include(CustomsBranches)`, `Include(ReceptionTransport)` e `Include(OperationalOrders)`, filtrando por `IsActive == true` y `Id == reception_entrance_id`.
3. Obtiene la primera coincidencia con `FirstOrDefaultAsync`.
4. Mapea con `ReceptionEntranceProfile` a `ReceptionEntranceDetailsDto` y lo devuelve.

> **Contexto de la request:** `company_id`, `module_code` y `reception_entrance_id` llegan por la ruta, y `user_id` se lee de `HttpContext.Items["UserId"]`.

> **Validación (FluentValidation):** el `GetReceptionEntranceDetailsValidator` está **vacío**. Solo se ejecutan las reglas de `BaseRequestValidator` (`CompanyId`, `ModuleCode`, `UserId`).

> **El query es *tracked*:** no aplica `AsNoTracking()`, a diferencia del listado. La entidad queda adjunta al contexto durante el request.

> **El `Include(User)` no se aprovecha.** El handler carga la navegación `User`, pero `ReceptionEntranceDetailsDto` **no tiene ninguna propiedad de usuario**, así que el dato se descarta en el mapeo y no aparece en la respuesta.

> ⚠️ **El handler no valida que la recepción exista.** Si el `reception_entrance_id` no corresponde a una recepción activa, `_mapper.Map<ReceptionEntranceDetailsDto>(null)` devuelve `null` y el endpoint responde **`200 OK` con el body `null`** en lugar de un `404`. También responde `200` con `null` si la recepción existe pero tiene `IsActive == false`. Verifícalo en el cliente antes de deserializar.

---

## Respuestas

### ✅ 200 OK

Devuelve un `ReceptionEntranceDetailsDto`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`). Las horas viajan como `HH:mm:ss`.

```json
{
  "reception_code": "REC-00045",
  "reception_entrance_id": "9a1f2c3d-4e5b-6a7f-8c9d-0e1f2a3b4c5d",
  "vehicle_plate_number": "MGA-1234",
  "vehicle_exit_time": null,
  "container_exit_time": null,
  "seal_number": "SEAL-8899",
  "container_number": "CONT-4455",
  "country_of_origin": "Panamá",
  "document_type": "DUCA",
  "created_at": "2026-09-21T10:15:00",
  "additional_data": "{\"evidence_urls\":[{\"image_id\":\"59b8f1eb-4a81-4131-ab8a-eb4f363549ca\",\"image_url\":\"https://erp-grupo-vassalli-develop.s3.us-east-1.amazonaws.com/warehouse/receptionentrance/iN2Wq17i2jlX.png\"},{\"image_id\":\"52d9d396-62c4-424e-9f5c-1ed7dd7e48d0\",\"image_url\":\"https://erp-grupo-vassalli-develop.s3.us-east-1.amazonaws.com/warehouse/receptionentrance/YXD04vOAnmZn.png\"}],\"document_numbers\":[{\"document_id\":\"13f5b7de-b707-41a7-8b61-05b4cb15f256\",\"document_type\":3,\"document_numbers\":\"DUCA-0001\",\"operational_order_id\":\"30b1b917-4ec4-4a81-84c3-d294c4ac7a15\"},{\"document_id\":\"2f7de01d-ee7f-486d-a317-aec2771e0886\",\"document_type\":3,\"document_numbers\":\"DUCA-0002\",\"operational_order_id\":\"f04cf56c-a7e2-4024-a729-676150552594\"}]}",
  "custom_branches_information": {
    "custom_branch_id": "f9c8c488-f53e-46c2-9594-1e9b23cf805c",
    "code": "ADU-MGA",
    "name": "Aduana de Managua"
  },
  "reception_transport_entrance_information": {
    "driver_name": "Juan Pérez",
    "driver_license": "P-123456",
    "transportista": "Transportes del Pacífico",
    "vehicle_plate_number": "MGA-1234",
    "vehicle_chassis_number": "CHASSIS-9988",
    "transport_unit": "Tractor"
  }
}
```

### Campos heredados de `ReceptionEntranceDto`

| Campo                   | Tipo                  | Descripción |
|-------------------------|-----------------------|-------------|
| `reception_code`        | `string`              | Código generado de la recepción. |
| `reception_entrance_id` | `guid`                | Id de la recepción. |
| `vehicle_plate_number`  | `string`              | Placa del vehículo. |
| `vehicle_exit_time`     | `time`                | Hora de salida del vehículo. Siempre `null`: ningún endpoint la escribe todavía. |
| `container_exit_time`   | `time`                | Hora de salida del contenedor. Siempre `null` por la misma razón. |
| `seal_number`           | `string`              | Número de sello. |
| `container_number`      | `string`              | Número de contenedor. |
| `country_of_origin`     | `string`              | País de origen. |
| `document_type`         | `enum (DocumentType)` | Tipo de documento, derivado de la primera orden operacional de la recepción. |

### Campos propios del detalle

| Campo                                       | Tipo                                       | Descripción |
|---------------------------------------------|--------------------------------------------|-------------|
| `created_at`                                | `datetime`                                 | Fecha de creación de la recepción. |
| `additional_data`                           | `string`                                   | **String JSON serializado**, no un objeto. Ver su estructura abajo. |
| `custom_branches_information`               | `object (CustomBranchesInformation)`       | Aduana de procedencia. |
| `reception_transport_entrance_information`  | `object (ReceptionTransportEntranceDto)`   | Datos completos del transporte. |

### Estructura de `additional_data`

Llega como **string**, hay que hacer una **doble deserialización**: el primer nivel es el JSON de la respuesta, el segundo es el contenido del string. El contenido se serializa con `JsonNamingPolicy.SnakeCaseLower`.

```json
{
  "evidence_urls": [
    {
      "image_id": "59b8f1eb-4a81-4131-ab8a-eb4f363549ca",
      "image_url": "https://erp-grupo-vassalli-develop.s3.us-east-1.amazonaws.com/warehouse/receptionentrance/iN2Wq17i2jlX.png"
    }
  ],
  "document_numbers": [
    {
      "document_id": "13f5b7de-b707-41a7-8b61-05b4cb15f256",
      "document_type": 3,
      "document_numbers": "DUCA-0001",
      "operational_order_id": "30b1b917-4ec4-4a81-84c3-d294c4ac7a15"
    }
  ]
}
```

| Campo del contenido                         | Tipo                          | Descripción |
|---------------------------------------------|-------------------------------|-------------|
| `evidence_urls`                             | `array`                       | Evidencias fotográficas subidas a S3. |
| `evidence_urls[].image_id`                  | `guid`                        | Id de la imagen. Es el valor que hay que enviar en `evidence_ids_to_delete` para eliminarla. |
| `evidence_urls[].image_url`                 | `string`                      | URL pública de la imagen en S3. |
| `document_numbers`                          | `array`                       | Documentos asociados a la recepción. |
| `document_numbers[].document_id`            | `guid`                        | Id interno del documento. Lo genera el handler, no el cliente. |
| `document_numbers[].document_type`          | `enum (DocumentType)`         | Tipo de documento. Se serializa como **número** porque el contenido del string usa `System.Text.Json` sin `JsonStringEnumConverter`. |
| `document_numbers[].document_numbers`       | `string`                      | Número del documento. En plural por diseño, pero siempre contiene un único string. |
| `document_numbers[].operational_order_id`   | `guid`                        | Id de la orden operativa asociada. Es el valor que debe reenviarse en `ducat_numbers[].operational_order_id` del PATCH para actualizar el documento. |

> ⚠️ **Los nombres son `image_id` e `image_url`, no `document_id` ni `document_url`.** Solo `document_numbers[]` usa el prefijo `document_`.

> ⚠️ **El `document_type` numérico dentro de `additional_data` no coincide con el `document_type` del nivel superior.** El nivel superior viaja como string (`"DUCA"`) porque lo serializa ASP.NET con `JsonStringEnumConverter`; el interior del string viaja como número porque lo serializa `System.Text.Json` directo. No interpretes el `3` como el valor del enum sin confirmarlo contra el paquete NuGet.

### Notas

| Campo / regla | Descripción |
|---|---|
| Orden no encontrada | Devuelve `200` con `null`, no `404`. |
| Rol | **Sin restricción.** A diferencia del registro y la actualización, este endpoint no comprueba el rol: es accesible para todos, incluido `Supervisor`. |
| Recepción inactiva | También devuelve `200` con `null`, porque el query filtra por `IsActive`. |
| Usuario de creación | **No se devuelve.** El handler hace `Include(User)` pero el DTO no expone ninguna propiedad de usuario. |
| `vehicle_exit_time` / `container_exit_time` | Siempre `null`. El controller tiene pendiente el endpoint de continuidad para el registro vehicular y salida de recepción. |
| `document_type` | Se resuelve desde `OperationalOrders.FirstOrDefault().DocumentType`. El query **sí incluye** `OperationalOrders`, por lo que el valor es correcto. Si no hay OPs, es el `default` del enum. |
| Evidencias | Se suben como Base64 en el registro y en la actualización, y se devuelven como URLs. El PATCH trabaja sobre los `image_id`. |
| `operational_order_id` | Es el ancla entre el documento de la recepción y su orden operativa. Es el valor que el front debe reenviar en el PATCH para actualizar el número de documento sin ambigüedad. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:005",
    "description": "No tienes acceso a este módulo"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `Validation_Error` | Errores de `BaseRequestValidator` (`CompanyId`, `ModuleCode`, `UserId`) o de binding si `reception_entrance_id` no es un GUID válido. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

> **Este endpoint no emite `ERP:INVALID_ACCESS`.** No hay comprobación de rol en el handler, así que la tabla de errores de rol de los otros endpoints no aplica aquí.

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `Forbidden` | `X-Api-Key` ausente o incorrecta. Lo devuelve el `ApiKeyMiddleware` antes de llegar al controller. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** (fuera de `CoreException`) el `ExceptionMiddleware` responde en **PascalCase**:

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "Server_Error",
    "Description": "Error interno no controlado."
  },
  "CreatedAt": "2026-09-30 14:32:10"
}
```

---

## Códigos de Estado

| Código | Descripción |
|--------|-------------|
| `200` | Detalle obtenido. Si el id no existe o la recepción está inactiva, también `200` pero con el body `null`. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

> **No existe `404`.** A diferencia del PATCH de recepción, este endpoint nunca devuelve `404` aunque la recepción no exista.

---

## Catálogos de Enums Utilizados

### `DocumentType`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain` y sus valores enteros no son legibles desde este repositorio, por lo que **no se documentan números**. Los miembros referenciados en el código del módulo son `DUCA` y `CustomsDeclaration`.

> En el ejemplo de `additional_data` el valor aparece como número porque es el contenido de un **string JSON producido por `System.Text.Json`**, que no tiene el `JsonStringEnumConverter` de ASP.NET. Ese `3` **no debe interpretarse** como el valor numérico del enum.

### `TransportUnit`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain`. Solo aparece en `reception_transport_entrance_information.transport_unit`.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Listado paginado. De ahí se obtiene el `reception_entrance_id`. |
| `PATCH /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` | Actualiza los datos de la recepción, incluidos los documentos y las evidencias. Usa `additional_data.document_numbers[].operational_order_id` para identificar cada orden operativa. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/custom-branches` | Listado de aduanas, para validar `custom_branch_id` antes de enviarlo. |