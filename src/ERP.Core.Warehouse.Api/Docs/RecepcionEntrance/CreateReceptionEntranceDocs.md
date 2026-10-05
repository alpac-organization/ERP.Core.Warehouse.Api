# Recepciones

## Registrar Recepción

Endpoint para registrar una recepción en el control de acceso. Genera la recepción, su información de transporte y las órdenes operacionales (PO) correspondientes al tipo de documento declarado.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` |
| **Descripción** | Registra una recepción y genera sus órdenes operacionales. |
| **Tags**        | `Control de Acceso` |

---

## Parámetros de Ruta (Path Params)

| Parámetro     | Tipo     | Requerido | Descripción |
|:------------:|:--------:|:---------:|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `X-Api-Key`     | `{api_key}`        | Sí        |
| `Content-Type`  | `application/json` | Sí        |

---

## Body

### Con tipo de documento `DUCA`

```json
{
  "general_information": {
    "custom_branch_id": "f9c8c488-f53e-46c2-9594-1e9b23cf805c",
    "seal_number": "SEAL-8899",
    "country_origin": "Panamá",
    "container_number": "CONT-4455",
    "document_type": "DUCA",
    "ducat_numbers": ["DUCA-000123", "DUCA-000124"],
    "customs_declaration_number": null
  },
  "transport_information": {
    "driver_name": "Juan Pérez",
    "driver_license": "P-123456",
    "transportista": "Transportes del Pacífico",
    "vehicle_plate_number": "MGA-1234",
    "vehicle_chassis_number": "CHASSIS-9988",
    "transport_unit": "Tractor"
  },
  "customs_declaration_information": null,
  "evidence_base64": ["data:image/jpeg;base64,/9j/4AAQSkZJRg..."]
}
```

### Con tipo de documento `CustomsDeclaration`

```json
{
  "general_information": {
    "custom_branch_id": "f9c8c488-f53e-46c2-9594-1e9b23cf805c",
    "seal_number": "SEAL-8899",
    "country_origin": "Panamá",
    "container_number": "CONT-4455",
    "document_type": "CustomsDeclaration",
    "ducat_numbers": [],
    "customs_declaration_number": "DEC-2026-0001"
  },
  "transport_information": {
    "driver_name": "Juan Pérez",
    "driver_license": "P-123456",
    "transportista": "Transportes del Pacífico",
    "vehicle_plate_number": "MGA-1234",
    "vehicle_chassis_number": "CHASSIS-9988",
    "transport_unit": "Tractor"
  },
  "customs_declaration_information": {
    "total_weight": 1250.50,
    "package_number": 42,
    "product_description": "Bobinas de acero inoxidable",
    "observations": "Sin observaciones"
  },
  "evidence_base64": []
}
```

| Campo                            | Tipo                                    | Requerido | Descripción |
|----------------------------------|-----------------------------------------|:---------:|-------------|
| `general_information`            | `object (GeneralInformation)`           | Sí        | Datos generales de la recepción. |
| `transport_information`          | `object (TransportInformation)`         | Sí        | Datos del transporte. |
| `customs_declaration_information` | `object (CustomsDeclarationInformation)` | Condicional | Obligatorio solo si `document_type` es `CustomsDeclaration`. Debe ser `null` si es `DUCA`. |
| `evidence_base64`                | `array (string)`                        | No        | Imágenes en Base64. Default `[]`. Se suben a S3. |

### `general_information`

| Campo                       | Tipo                  | Requerido | Descripción |
|-----------------------------|-----------------------|:---------:|-------------|
| `custom_branch_id`          | `guid`                | No        | Aduana de procedencia. **No se valida contra la base de datos en este endpoint.** |
| `seal_number`               | `string`              | No        | Número de sello. |
| `country_origin`            | `string`              | No        | País de origen. |
| `container_number`          | `string`              | No        | Número de contenedor. |
| `document_type`             | `enum (DocumentType)` | Sí        | `DUCA` o `CustomsDeclaration`. |
| `ducat_numbers`             | `array (string)`      | Condicional | Obligatorio y no vacío si `document_type` es `DUCA`. Debe estar vacío si es `CustomsDeclaration`. |
| `customs_declaration_number` | `string`             | Condicional | Obligatorio si `document_type` es `CustomsDeclaration`. |

### `transport_information`

| Campo                   | Tipo                  | Requerido | Descripción |
|-------------------------|-----------------------|:---------:|-------------|
| `driver_name`           | `string`              | No        | Nombre del conductor. |
| `driver_license`        | `string`              | No        | Licencia del conductor. |
| `transportista`         | `string`              | No        | Nombre del transportista. |
| `vehicle_plate_number`  | `string`              | No        | Placa del vehículo. |
| `vehicle_chassis_number` | `string`             | No        | Número de chasis. |
| `transport_unit`        | `enum (TransportUnit)` | No      | Tipo de unidad de transporte. |

### `customs_declaration_information`

| Campo                 | Tipo      | Requerido | Descripción |
|-----------------------|-----------|:---------:|-------------|
| `total_weight`        | `decimal` | Sí        | Peso total. Debe ser **mayor que cero**. |
| `package_number`      | `decimal` | Sí        | Número de bultos. Debe ser **mayor que cero**. |
| `product_description` | `string`  | No        | Descripción del producto. Se copia a la asignación operativa. |
| `observations`        | `string`  | No        | Observaciones. Se copian a la asignación operativa. |

---

## Flujo del Handler (`CreateReceptionEntranceHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Si el rol del usuario es `Supervisor`, devuelve `401` con `ERP:INVALID_ACCESS`.
3. Construye la entidad de recepción (`IsActive = true`, `Id = Guid.NewGuid()`) y asigna `CreatedByUserId` con el usuario del token.
4. Genera un código de recepción único con `GenerateUniqueReceptionEntranceCodeAsync`. Si falla devuelve `500`.
5. Por cada elemento de `evidence_base64`, sube la imagen a S3 (`Warehouse/ReceptionEntrance`) y agrega su `image_id` / `image_url` a `additional_data`.
6. Registra los documentos en `additional_data`: un item por cada número de DUCA, o uno solo con el número de declaración aduanera.
7. Serializa `additional_data` a JSON con `SnakeCaseLower` y lo guarda en la recepción.
8. Inserta la recepción y la información de transporte.
9. Según el `document_type`, genera las órdenes operacionales:

### Rama `DUCA`

10. **Por cada** número de `ducat_numbers`, crea una orden operacional con:
    - `DocumentType = DUCA`, `Status = PendingDocument`
    - `DocumentNumber` = ese número de DUCA
    - `CostCenterId` = el centro de costos del perfil de acceso
    - `IsConsolidated = true` si hay más de un número de DUCA
    - `CompanyId` = compañía de la ruta, `ReceptionId` = id de la recepción
11. Genera un `po_code` único por orden con `GenerateUniqueOperationalOrderCodeAsync`.
12. Guarda los cambios.

### Rama `CustomsDeclaration`

10. Crea **una sola** orden operacional con `DocumentType = CustomsDeclaration`, `Status = PendingDocument` y `CostCenterId` del perfil de acceso.
11. Si viene `customs_declaration_information`:
    - Construye una asignación operativa con `Status = Pending`, `Observations`, `MerchandiseDescription = product_description`, `Category = None`, `DestinationType = None` y `WarehouseId = null`.
    - **Valida la ventana horaria de aduana.** Si está fuera, devuelve `400`.
    - Copia `total_weight` a `Weight` y `package_number` a `PackagesCount` de la orden.
12. Asigna `DocumentNumber = customs_declaration_number` y genera el `po_code`.
13. Si hubo asignación operativa, marca la orden con `HasAssignmentOperationalActive = true` y la registra.
14. Guarda los cambios.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta y el controller los asigna al `payload`. `user_id` se lee de `HttpContext.Items["UserId"]`.

> **Validación (FluentValidation):** el `CreateReceptionEntranceValidator` valida que `general_information` y `transport_information` no sean `null`, y aplica reglas condicionales según `document_type`. **No valida** `custom_branch_id`, ni la existencia de la aduana, ni los campos de texto, ni los de transporte, ni `evidence_base64`.

### Reglas condicionales del validator

| Condición | Reglas |
|-----------|--------|
| `document_type == DUCA` | `ducat_numbers` no vacío: `Debe indicar al menos un número de DUCA cuando el tipo de documento es DUCA.` &nbsp;·&nbsp; Cada elemento no vacío: `Los números de DUCA no pueden estar vacíos.` &nbsp;·&nbsp; `customs_declaration_information` **nulo**: `No debe enviar información de declaración aduanera cuando el tipo de documento es DUCA.` |
| `document_type == CustomsDeclaration` | `ducat_numbers` vacío: `La lista de DUCA debe estar vacía cuando el tipo de documento es Declaración Aduanera.` &nbsp;·&nbsp; `customs_declaration_information` no nulo: `Debe enviar la información de declaración aduanera.` &nbsp;·&nbsp; `customs_declaration_number` no vacío: `Debe indicar el número de declaración aduanera.` &nbsp;·&nbsp; Si no es nulo: `total_weight > 0` y `package_number > 0`. |

### Ventana horaria de declaración aduanera

Cuando `document_type` es `CustomsDeclaration` **y** viene `customs_declaration_information`, el handler exige estar dentro de una de estas dos ventanas:

| Ventana | Horario |
|---------|---------|
| Nocturna | De **17:00 a 08:00** (cruza medianoche) |
| Mediodía | De **12:00 a 13:00** |

> La comparación usa `DateTime.Now.TimeOfDay`, es decir, la **hora local del servidor**, no una zona horaria de negocio ni el reloj de Nicaragua. El límite de las 13:00 es **inclusivo**, y el de las 08:00 también.

> ⚠️ **La ventana solo se valida si viene `customs_declaration_information`.** Una recepción `CustomsDeclaration` sin ese objeto no pasa por la validación horaria.

### ⚠️ Generación de órdenes operacionales

Este endpoint **no solo registra recepciones**: también crea las PO. Es el punto de entrada del módulo de órdenes operacionales.

| Tipo de documento | Órdenes creadas | Asignación operativa |
|-------------------|------------------|----------------------|
| `DUCA` | **Una por cada** número de DUCA | Ninguna |
| `CustomsDeclaration` | **Exactamente una** | Una con `Status = Pending`, solo si viene `customs_declaration_information` |

> Para consultar las PO generadas, usa el [listado de órdenes operacionales](../OperationalOrders/GetOperationalOrdersDocs.md).

---

## Respuestas

### ✅ 201 Created

El controller responde `Created()` **sin argumentos**, por lo que la respuesta **no incluye body** ni cabecera `Location` con la URL del recurso creado. No hay forma de obtener el `reception_entrance_id` generado desde esta respuesta.

```json
{}
```

> Los ids y códigos generados (`reception_code`, `reception_entrance_id`, `po_code`) **no se devuelven**. Para recuperarlos hay que consultar el [listado de recepciones](GetReceptionEntrancesDocs.md) o el de órdenes operacionales.

### Notas

| Campo / regla | Descripción |
|---|---|
| Rol | `Supervisor` bloqueado con `401`. |
| `custom_branch_id` | **No se valida.** Puede referenciar una aduana inexistente y el registro se guarda igual. El PATCH sí valida la existencia. |
| `IsConsolidated` | Se calcula como `ducat_numbers.Count > 1`, solo en la rama `DUCA`. |
| `Status` de la PO | Siempre `PendingDocument`. |
| `CostCenterId` | Proviene del **perfil de acceso del usuario**, no del body. No es posible elegirlo desde la request. |
| `CreatedByUserId` | Se toma del token, no del body. |
| Evidencias | Base64 en el request, URLs de S3 en `additional_data`. |
| Salida de vehículo / contenedor | **No se registra.** El controller deja pendiente el `//Endpoint para darle continuidad al registro vehicular y salid de reception.` |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:CUSTOMS_DECLARATION_OUT_OF_WINDOW",
    "description": "La información de declaración de aduana solo puede registrarse de 5:00 pm a 8:00 am o de 12:00 pm a 1:00 pm"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:CUSTOMS_DECLARATION_OUT_OF_WINDOW` | `La información de declaración de aduana solo puede registrarse de 5:00 pm a 8:00 am o de 12:00 pm a 1:00 pm` |
| `ERP:INVALID_DOCUMENT` | `Error al registrar la información, el tipo de documento no es aceptable`. Se emite si `document_type` es un valor del enum distinto de `DUCA` y `CustomsDeclaration`. |
| `Validation_Error` | Errores del `CreateReceptionEntranceValidator` listados arriba, más los de `BaseRequestValidator`. |
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

### ❌ 500 Internal Server Error

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "ERP:CODE_GENERATOR_ERROR",
    "Description": "Ocurrio un error al generar codigo de recepción"
  },
  "CreatedAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:CODE_GENERATOR_ERROR` | `Ocurrio un error al generar codigo de recepción`. Falló `GenerateUniqueReceptionEntranceCodeAsync`. |
| `ERP:INTERNAL_ERROR` | `Ocurrio un error al generar la generación de archivo`. Falló `GenerateUniqueOperationalOrderCodeAsync`. |
| `Server_Error` | Excepción no controlada. Se responde en **PascalCase**. |

---

## Códigos de Estado

| Código | Descripción |
|--------|-------------|
| `201` | Recepción registrada. Sin body. |
| `400` | Error de validación, de tipo de documento, o fuera de la ventana horaria (`ErrorResponse` camelCase). |
| `401` | Rol `Supervisor` (`ERP:INVALID_ACCESS`) o token inválido. |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Fallo del generador de códigos (`ERP:CODE_GENERATOR_ERROR`, `ERP:INTERNAL_ERROR`) o error no controlado. |

---

## Catálogos de Enums Utilizados

### `DocumentType`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain` y sus valores enteros no son legibles desde este repositorio. La documentación anterior de este módulo asignaba `1`/`2` aquí y `3`/`4` en la actualización, lo que es internamente contradictorio: **no se documentan números**.

| Miembro              | DUCAs | Declaración aduanera | Comportamiento |
|----------------------|:-----:|:--------------------:|----------------|
| `DUCA`               | Obligatorio, ≥ 1 | Debe estar vacío | Una orden operacional por número de DUCA. Sin asignación operativa. Sin ventana horaria. |
| `CustomsDeclaration` | Debe estar vacío | Obligatorio | Una única orden operacional. Con asignación operativa si viene el detalle. Requiere ventana horaria. |

### `TransportUnit`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain`.

### `AssignmentOperationalStatus`

> ⚠️ **Confirmar valores numéricos.** Este endpoint crea la asignación operativa con `Status = AssignmentOperationalStatus.Pending`. El PATCH de órdenes operacionales usa `None` para el mismo tipo de entidad.

### `MerchandiseCategory` y `DestinationType`

> ⚠️ **Confirmar valores numéricos.** La asignación operativa se crea con `Category = MerchandiseCategory.None` y `DestinationType = DestinationType.None`.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Listado paginado, para localizar la recepción recién creada. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details` | Detalle completo, con `additional_data` y evidencias. |
| `PATCH /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` | Actualiza la recepción. Solo dentro de los primeros 10 minutos. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/operational-orders` | Lista las PO generadas por este registro. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/custom-branches` | Listado de aduanas, para obtener un `custom_branch_id` válido. |