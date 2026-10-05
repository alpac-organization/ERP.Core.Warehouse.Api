# Recepciones

## Actualizar Recepción

Endpoint para actualizar los datos generales, los documentos, las evidencias y la información de transporte de una recepción existente. Disponible durante los primeros 10 minutos desde su creación.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` |
| **Descripción** | Actualiza parcialmente una recepción dentro de la ventana de 10 minutos. |
| **Tags**        | `Control de Acceso` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `reception_entrance_id` | `guid`  | Sí        | Identificador único de la recepción. |

> El controller declara además un parámetro `reception_id` que **no está en la plantilla de la ruta** y no se usa. El id efectivo es el de `reception_entrance_id`.

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `X-Api-Key`     | `{api_key}`        | Sí        |
| `Content-Type`  | `application/json` | Sí        |

---

## Body

```json
{
  "general_information": {
    "custom_branch_id": "f9c8c488-f53e-46c2-9594-1e9b23cf805c",
    "seal_number": "SEAL-8899",
    "country_origin": "Panamá",
    "container_number": "CONT-4455",
    "document_type": "DUCA",
    "ducat_numbers": ["DUCA-000123", "DUCA-000125"],
    "customs_declaration_number": null
  },
  "reception_transport_information": {
    "driver_name": "Juan Pérez",
    "driver_license": "P-123456",
    "transportista": "Transportes del Pacífico",
    "vehicle_plate_number": "MGA-1234",
    "vehicle_chassis_number": "CHASSIS-9988",
    "transport_unit": "Tractor"
  },
  "evidence_base64": [],
  "evidence_ids_to_delete": ["1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d"]
}
```

| Campo                             | Tipo                                      | Requerido | Default | Descripción |
|-----------------------------------|-------------------------------------------|:---------:|---------|-------------|
| `general_information`             | `object (GeneralInformationUpdated)`      | No        | `null`  | Datos generales. Si se envía `null`, se omite todo este bloque. |
| `reception_transport_information` | `object (ReceptionTransportInformation)`  | No        | `null`  | Datos de transporte. Si se envía `null`, se omite todo este bloque. |
| `evidence_base64`                 | `array (string)`                          | No        | `[]`    | Nuevas imágenes en Base64. Se suben a S3. |
| `evidence_ids_to_delete`          | `array (guid)`                            | No        | `[]`    | `image_id` de las evidencias a eliminar, tomados de `additional_data`. |

### `general_information`

| Campo                       | Tipo                  | Requerido | Descripción |
|-----------------------------|-----------------------|:---------:|-------------|
| `custom_branch_id`          | `guid`                | No        | Aduana de procedencia. Solo se valida si es distinto de `Guid.Empty`. |
| `seal_number`               | `string`              | No        | Si viene `null`, se conserva el valor actual. |
| `country_origin`            | `string`              | No        | Si viene `null`, se conserva el valor actual. |
| `container_number`          | `string`              | No        | Si viene `null`, se conserva el valor actual. |
| `document_type`             | `enum (DocumentType)` | No        | Tipo de documento objetivo. **Suena obligatorio por la lógica del handler.** Ver la nota de abajo. |
| `ducat_numbers`             | `array (string)`      | No        | Números de DUCA. Default `[]`. |
| `customs_declaration_number` | `string`             | No        | Número de declaración aduanera. |

### `reception_transport_information`

| Campo                    | Tipo                  | Requerido | Descripción |
|--------------------------|-----------------------|:---------:|-------------|
| `driver_name`            | `string`              | No        | Si viene `null`, se conserva el actual. |
| `driver_license`         | `string`              | No        | Si viene `null`, se conserva el actual. |
| `transportista`          | `string`              | No        | Si viene `null`, se conserva el actual. |
| `vehicle_plate_number`   | `string`              | No        | Si viene `null`, se conserva el actual. |
| `vehicle_chassis_number` | `string`              | No        | Si viene `null`, se conserva el actual. |
| `transport_unit`         | `enum (TransportUnit)` | No      | **Se acepta en el body pero el handler nunca lo escribe.** Ver la nota de abajo. |

---

## Flujo del Handler (`UpdateReceptionEntranceHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Si el rol del usuario es `Supervisor`, devuelve `401` con `ERP:INVALID_ACCESS`.
3. Busca la recepción con `IsActive == true` y `Id == reception_entrance_id`. Si no existe devuelve `404`.
4. Calcula los minutos transcurridos desde `CreatedAt` y, si son **≥ 10**, devuelve `400`.
5. Deserializa `additional_data` y determina el tipo de documento actual a partir del primer item de `document_numbers`.
6. Si viene `general_information`:
   - Actualiza `seal_number`, `country_of_origin` y `container_number`, conservando el valor actual cuando llegan `null`.
   - Si `custom_branch_id != Guid.Empty`, valida que la aduana exista y esté activa. Si no, devuelve `400`.
   - Compara `document_type` con el actual. Si difieren, valida la transición y reescribe los documentos. Si son iguales, reescribe igualmente.
7. Si hay `evidence_base64` o `evidence_ids_to_delete`, actualiza las evidencias: elimina las indicadas y sube las nuevas.
8. Serializa `additional_data` y lo guarda.
9. Si viene `reception_transport_information`, busca el registro de transporte, actualiza sus campos y lo marca para guardar.
10. Guarda los cambios con `SaveChangesAsync` y devuelve `200 OK`.

> **Contexto de la request:** `company_id`, `module_code` y `reception_entrance_id` llegan por la ruta y el controller los asigna al `payload`. `user_id` se lee de `HttpContext.Items["UserId"]`.

> **Validación (FluentValidation):** el `UpdateReceptionEntranceValidator` está **vacío**. Solo se ejecutan las reglas de `BaseRequestValidator`. **Ningún** campo del body se valida, ni siquiera los valores de los enums.

### Comportamiento de la ventana de 10 minutos

| Condición | Resultado |
|-----------|-----------|
| `minutos_transcurridos < 10` | Continúa con la actualización. |
| `minutos_transcurridos >= 10` | `400` con `ERP:RECEPTION_UPDATE_TIME_EXPIRED`. |

> ⚠️ **La condición de rol de la ventana está mal escrita y afecta a todos los usuarios.** El código es:
> ```csharp
> if (minutesElapsed >= 10 && (RoleType != Administrator || RoleType != Manager))
> ```
> El `||` hace que la expresión de rol sea **siempre verdadera** (un rol no puede ser a la vez `Administrator` y `Manager`). En consecuencia, la condición se reduce a `minutesElapsed >= 10` y **la ventana de 10 minutos bloquea también a administradores y managers**, que en teoría deberían poder editar siempre.
>
> La documentación anterior de este endpoint afirmaba lo contrario. El comportamiento real es el descrito en la tabla.

> La medición usa `DateTime.UtcNow - CreatedAt` en **minutos**, mientras que `CreatedAt` se escribe con la hora del servidor. Si el servidor no está en UTC, el cálculo arrastra un desfase constante.

### Comportamiento de `document_type`

`DocumentType` en el command es **no nulable**, así que si se omite en el JSON llega con el valor `default` del enum. El handler compara ese valor contra el tipo actual de la recepción:

| Situación | Comportamiento |
|------------|----------------|
| `document_type` coincide con el actual | Reescribe `document_numbers` con los valores enviados. |
| `document_type` difiere y es `DUCA` | Exige `ducat_numbers` con al menos un elemento. Elimina los documentos `DUCA` previos y agrega uno por cada número recibido. |
| `document_type` difiere y es `CustomsDeclaration` | Exige `customs_declaration_number`. Elimina los documentos `CustomsDeclaration` previos y agrega el nuevo. |
| `document_type` omitido | Llega `default`, que **no coincide** con el tipo actual, así que entra en la rama de transición y falla con `ERP:INVALID_DOCUMENT_TYPE`. |

> ⚠️ **`document_type` es obligatorio de facto** dentro de `general_information`. Omitirlo no conserva el valor actual: provoca un `400`. Envíalo siempre.

> ⚠️ **Los documentos se reescriben siempre, incluso si no cambian.** Cuando el tipo coincide, el handler **borra** todos los documentos del tipo actual y vuelve a crear uno por cada número recibido. No es una actualización ni una fusión: es un reemplazo.

| Situación | Resultado sobre `document_numbers` |
|-----------|------------------------------------|
| `DUCA` actual, `DUCA` enviado con `ducat_numbers: ["A","B"]` | Borra los DUCA previos. Quedan exactamente `A` y `B`. |
| `DUCA` actual, `DUCA` enviado con `ducat_numbers: []` | **Borra todos los documentos y no agrega ninguno.** La recepción se queda sin documentos. |
| `CustomsDeclaration` actual, mismo tipo, con `customs_declaration_number: "DEC-1"` | Borra el previo. Queda `DEC-1`. |
| `CustomsDeclaration` actual, mismo tipo, con el número en `null` | **Borra el documento y no agrega otro.** La recepción se queda sin documentos. |

> Para modificar un solo campo (por ejemplo el número de sello) y **no tocar los documentos**, no envíes `general_information` completo: al enviarlo, los documentos se regeneran con los `ducat_numbers` y `customs_declaration_number` que incluyas. Reenvía siempre la lista completa de documentos actuales para no perderlos.

> Los nuevos documentos reciben un `document_id` nuevo (`Guid.NewGuid()`) en cada actualización. Cualquier referencia externa que haya guardado el `document_id` anterior queda obsoleta.

> ⚠️ **Al cambiar los documentos no se actualizan las órdenes operacionales.** El handler solo reescribe `additional_data.document_numbers`; las PO generadas en el registro conservan su `document_number` original. El resultado puede quedar inconsistente: la recepción muestra un DUCA nuevo mientras la PO sigue apuntando al anterior.

### Comportamiento de las evidencias

| Operación | Efecto |
|-----------|--------|
| `evidence_ids_to_delete` con un `image_id` existente | Elimina el item de `additional_data.evidence_urls`. |
| `evidence_ids_to_delete` con un `image_id` inexistente | No hace nada, sin error. |
| `evidence_base64` con imágenes | Las sube a S3 y las agrega con un `image_id` nuevo. |
| Ambos vacíos | No se toca el bloque de evidences. |

> ⚠️ **Eliminar una evidencia solo la quita de `additional_data`.** El objeto en S3 no se borra del bucket: queda huérfano.

> Las eliminaciones se aplican **antes** de las inserciones. Los `image_id` a eliminar se leen de `evidence_urls[].image_id` en la respuesta del detalle.

---

## Respuestas

### ✅ 200 OK

No devuelve body. El controller responde `Ok()` tras completar el `SaveChangesAsync`.

```json
{}
```

### Notas

| Campo / regla | Descripción |
|---|---|
| Actualización parcial | Cada bloque (`general_information`, `reception_transport_information`) es independiente. Omitir uno lo deja intacto. |
| Conservación de valores | Dentro de cada bloque, los strings `null` conservan el valor actual. Hay que enviar `""` para **borrar** un valor. |
| Rol | `Supervisor` bloqueado con `401`. |
| `custom_branch_id` | Solo se valida si es distinto de `Guid.Empty`. Omitirlo o mandar el Guid vacío **salta la validación** y conserva el valor actual. |
| `transport_unit` | **Aceptado pero ignorado.** El bloque de actualización del transporte no escribe este campo. No produce error, pero tampoco efecto. |
| Evidencias | Se suben y eliminan por `image_id`. Los objetos de S3 no se eliminan. |
| Ordenes operacionales | **No se tocan.** Los cambios de documentos no se propagan a las PO. |
| Sin body | Para verificar los cambios hay que consultar el detalle. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:RECEPTION_UPDATE_TIME_EXPIRED",
    "description": "Ya no se puede modificar la información vehicular de la recepción"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:RECEPTION_UPDATE_TIME_EXPIRED` | `Ya no se puede modificar la información vehicular de la recepción`. Se emite a los 10 minutos o más desde la creación, **para todos los roles**. |
| `ERP:ERROR_CUSTOM_BRANCH` | `La aduana de procendencia que desea actualizar no esta registrada`. El `custom_branch_id` no existe o está inactiva. |
| `ERP:ERROR_UPDATED` | `La información de transporte no se ha encontrado`. No hay registro de transporte asociado a la recepción. |
| `ERP:MISSING_DUCA_NUMBERS` | `Debe proporcionar al menos un número de DUCA`. Transición a `DUCA` sin `ducat_numbers`. |
| `ERP:MISSING_CUSTOMS_DECLARATION` | `El número de declaración aduanera es obligatorio`. Transición a `CustomsDeclaration` sin número. |
| `ERP:INVALID_DOCUMENT_TYPE` | `Tipo de documento no válido`. Se emite cuando `document_type` llega con el valor `default`, normalmente por **omitirlo** en el body. |
| `Validation_Error` | Errores de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`) o de binding del body. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 401 Unauthorized

| `typeError` | `description` |
|-------------|---------------|
| `ERP:INVALID_ACCESS` | `No tienes acceso a realizar esta acción`. Se emite únicamente cuando el rol del usuario es `Supervisor`. |
| `Unauthorized` | Token ausente, inválido o expirado. Lo devuelve el `AuthMiddleware`. |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `Forbidden` | `X-Api-Key` ausente o incorrecta. Lo devuelve el `ApiKeyMiddleware` antes de llegar al controller. |

### ❌ 404 Not Found

```json
{
  "status": 404,
  "error": {
    "typeError": "ERP:NOT_FOUND_RECEPTION",
    "description": "La reception a actualizar no existe registrada"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:NOT_FOUND_RECEPTION` | `La reception a actualizar no existe registrada`. La recepción no existe o tiene `IsActive == false`. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** el `ExceptionMiddleware` responde en **PascalCase**:

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
| `200` | Recepción actualizada. Sin body. |
| `400` | Fuera de la ventana de 10 minutos, aduana inexistente, documentos incompletos, tipo de documento inválido, o transporte ausente. |
| `401` | Rol `Supervisor` (`ERP:INVALID_ACCESS`) o token inválido. |
| `403` | `X-Api-Key` ausente o inválida. |
| `404` | La recepción no existe o está inactiva (`ERP:NOT_FOUND_RECEPTION`). |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `DocumentType`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain` y sus valores enteros no son legibles desde este repositorio. La documentación anterior de este endpoint asignaba `3` a `DUCA` y `4` a `CustomsDeclaration`, mientras que el registro usaba `1` y `2` para los mismos miembros. **Una de las dos afirmaciones es incorrecta y no hay forma de verificarlo sin descompilar el paquete**, así que no se documentan números.

Los únicos dos miembros que el handler acepta son `DUCA` y `CustomsDeclaration`. Cualquier otro valor, incluido el `default` por omisión, produce `ERP:INVALID_DOCUMENT_TYPE`.

| Miembro              | Requisito al cambiar a este tipo |
|----------------------|----------------------------------|
| `DUCA` | `ducat_numbers` con al menos un elemento. |
| `CustomsDeclaration` | `customs_declaration_number` no vacío. |

### `TransportUnit`

> ⚠️ **Confirmar valores numéricos.** El enum se acepta en `reception_transport_information.transport_unit` pero **el handler nunca lo escribe**. Su valor efectivo en la base de datos solo cambia con un `POST` de recepción nuevo.

### `RoleType`

> ⚠️ **Confirmar valores numéricos.** El handler compara contra `Supervisor`, `Administrator` y `Manager`. La comparación con `Administrator`/`Manager` está presente pero **no tiene efecto** por el error de `||` descrito arriba.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Listado paginado. De ahí se obtiene el `reception_entrance_id`. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details` | Detalle completo, con `additional_data`, `document_numbers` y `evidence_urls`. Necesario para conocer los `image_id` a eliminar. |
| `POST /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Registra la recepción y abre la ventana de 10 minutos. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/custom-branches` | Listado de aduanas, para validar `custom_branch_id`. |