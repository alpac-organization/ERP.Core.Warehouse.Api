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

| Parámetro             | Tipo     | Requerido | Descripción |
|:---------------------:|:--------:|:---------:|-------------|
| `company_id`          | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`         | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `reception_entrance_id` | `guid` | Sí        | Identificador único de la recepción. Es el valor que devuelve `reception_entrance_id` en el listado. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `X-Api-Key`     | `{api_key}`      | Sí        |

---

## Flujo del Handler (`GetReceptionEntranceDetailsHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Busca la recepción con `Include(User)`, `Include(CustomsBranches)`, `Include(ReceptionTransport)`, filtrando por `IsActive == true` y `Id == reception_entrance_id`.
3. Obtiene la primera coincidencia con `FirstOrDefaultAsync`.
4. Mapea con `ReceptionEntranceProfile` a `ReceptionEntranceDetailsDto` y lo devuelve.

> **Contexto de la request:** `company_id`, `module_code` y `reception_entrance_id` llegan por la ruta, y `user_id` se lee de `HttpContext.Items["UserId"]`.

> **Validación (FluentValidation):** el `GetReceptionEntranceDetailsValidator` está **vacío**. Solo se ejecutan las reglas de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`).

> **El query es *tracked*:** no aplica `AsNoTracking()`, a diferencia del listado. La entidad queda adjunta al contexto durante el request.

> **El `Include(User)` no se aprovecha.** El handler carga la navegación `User`, pero `ReceptionEntranceDetailsDto` **no tiene ninguna propiedad de usuario**, así que el dato se descarta en el mapeo y no aparece en la respuesta.

> ⚠️ **El handler no valida que la recepción exista.** Si el `reception_entrance_id` no corresponde a una recepción activa, `_mapper.Map<ReceptionEntranceDetailsDto>(null)` devuelve `null` y el endpoint responde **`200 OK` con el body `null`** en lugar de un `404`. También responde `200` con `null` si la recepción existe pero tiene `IsActive == false`. Verifícalo en el cliente antes de deserializar.

### ⚠️ Parámetro de ruta sobrante

La acción del controller declara un parámetro `reception_id` que **no está en la plantilla de la ruta** y **nunca se usa**: el id real se recibe en `reception_entrance_id`. Está declarado en la firma por heredad de una versión anterior de la API y no afecta el comportamiento del endpoint.

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
  "additional_data": "{\"document_numbers\":[{\"document_id\":\"7c2e9a11-5b3d-4c8e-9f01-2a3b4c5d6e7f\",\"document_numbers\":\"DUCA-000123\",\"document_type\":1},{\"document_id\":\"8d3f0b22-6c4e-4d9f-a012-3b4c5d6e7f80\",\"document_numbers\":\"DUCA-000124\",\"document_type\":1}],\"evidence_urls\":[{\"image_id\":\"1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d\",\"image_url\":\"https://s3.amazonaws.com/warehouse/reception/evidencia-1.jpg\"}]}",
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

| Campo                                  | Tipo                                | Descripción |
|----------------------------------------|-------------------------------------|-------------|
| `created_at`                           | `datetime`                          | Fecha de creación de la recepción. |
| `additional_data`                      | `string`                            | **String JSON serializado**, no un objeto. Ver su estructura abajo. |
| `custom_branches_information`          | `object (CustomBranchesInformation)`| Aduana de procedencia. |
| `reception_transport_entrance_information` | `object (ReceptionTransportEntranceDto)` | Datos completos del transporte. |

### Estructura de `additional_data`

Llega como **string**, hay que hacer una **doble deserialización**: el primer nivel es el JSON de la respuesta, el segundo es el contenido del string.

```json
{
  "document_numbers": [
    {
      "document_id": "7c2e9a11-5b3d-4c8e-9f01-2a3b4c5d6e7f",
      "document_numbers": "DUCA-000123",
      "document_type": 1
    }
  ],
  "evidence_urls": [
    {
      "image_id": "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d",
      "image_url": "https://s3.amazonaws.com/warehouse/reception/evidencia-1.jpg"
    }
  ]
}
```

| Campo del contenido            | Tipo       | Descripción |
|--------------------------------|------------|-------------|
| `document_numbers`             | `array`    | Documentos asociados a la recepción. |
| `document_numbers[].document_id` | `guid`  | Id interno del documento. Lo genera el handler, no el cliente. |
| `document_numbers[].document_numbers` | `string` | Número del documento. En plural por diseño, pero siempre contiene un único string. |
| `document_numbers[].document_type` | `enum (DocumentType)` | Tipo de documento. |
| `evidence_urls`                | `array`    | Evidencias fotográficas subidas a S3. |
| `evidence_urls[].image_id`     | `guid`     | Id de la imagen. Es el valor que hay que enviar en `evidence_ids_to_delete` para eliminarla. |
| `evidence_urls[].image_url`    | `string`   | URL de la imagen en S3. |

> ⚠️ **Los nombres son `image_id` e `image_url`, no `document_id` ni `document_url`.** La documentación anterior de este endpoint los tenía equivocados. Solo `document_numbers[]` usa el prefijo `document_`.

### Notas

| Campo / regla | Descripción |
|---|---|
| Orden no encontrada | Devuelve `200` con `null`, no `404`. |
| Rol | **Sin restricción.** A diferencia del registro y la actualización, este endpoint no comprueba el rol: es accesible para todos, incluido `Supervisor`. |
| Recepción inactiva | También devuelve `200` con `null`, porque el query filtra por `IsActive`. |
| Usuario de creación | **No se devuelve.** El handler hace `Include(User)` pero el DTO no expone ninguna propiedad de usuario. |
| `vehicle_exit_time` / `container_exit_time` | Siempre `null`. El controller tiene el comentario `//Endpoint para darle continuidad al registro vehicular y salid de reception.` pendiente de implementar. |
| `document_type` | No es una columna de la recepción: el mapper lo deriva de `OperationalOrders.FirstOrDefault().DocumentType`, y para eso el query de este handler **no** incluye `OperationalOrders`. Al no estar cargada la navegación, el valor resuelto es el `default` del enum. |
| Evidencias | Se suben como Base64 en el registro y en la actualización, y se devuelven como URLs. El PATCH trabaja sobre los `image_id`. |

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
| `Validation_Error` | Errores de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`) o de binding si `reception_entrance_id` no es un GUID válido. |
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

> En el ejemplo de `additional_data` el valor aparece como número porque es el contenido de un **string JSON producido por `System.Text.Json`**, que no tiene el `JsonStringEnumConverter` de ASP.NET. Ese `1` **no debe interpretarse** como el valor numérico del enum.

### `TransportUnit`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain`. Solo aparece en `reception_transport_entrance_information.transport_unit`.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Listado paginado. De ahí se obtiene el `reception_entrance_id`. |
| `PATCH /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` | Actualiza los datos de la recepción, incluidos los documentos y las evidencias. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/custom-branches` | Listado de aduanas, para validar `custom_branch_id` antes de enviarlo. |