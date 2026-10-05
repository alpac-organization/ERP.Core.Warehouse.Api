# Recepciones

## Actualizar Recepción

Endpoint para actualizar la información general, los documentos, las evidencias fotográficas y la información de transporte de una recepción existente.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` |
| **Descripción** | Actualiza parcialmente una recepción. Los documentos (DUCA o declaración aduanera) se sincronizan contra las órdenes operativas por su `operational_order_id`. |
| **Tags**        | `Control de Acceso` |

---

## Parámetros de Ruta (Path Params)

| Parámetro               | Tipo     | Requerido | Descripción |
|:-----------------------:|:--------:|:---------:|-------------|
| `company_id`            | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`           | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `reception_entrance_id` | `guid`   | Sí        | Identificador único de la recepción. |

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

Todos los campos son opcionales. Solo se aplican los que vienen con valor.

```json
{
  "general_information": {
    "custom_branch_id": "f9c8c488-f53e-46c2-9594-1e9b23cf805c",
    "seal_number": "SEAL-8899",
    "country_origin": "Panamá",
    "container_number": "CONT-4455",
    "document_type": "DUCA",
    "ducat_numbers": [
      {
        "operational_order_id": "30b1b917-4ec4-4a81-84c3-d294c4ac7a15",
        "document_number": "DUCA-0009"
      },
      {
        "operational_order_id": "f04cf56c-a7e2-4024-a729-676150552594",
        "document_number": "DUCA-0006"
      }
    ],
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
  "evidence_base64": [
    "data:image/png;base64,iVBORw0KGgoAAAANSUhEUg..."
  ],
  "evidence_ids_to_delete": [
    "1a2b3c4d-5e6f-7a8b-9c0d-1e2f3a4b5c6d"
  ]
}
```

---

## `general_information`

| Campo                        | Tipo                              | Requerido | Default | Descripción |
|------------------------------|-----------------------------------|:---------:|---------|-------------|
| `custom_branch_id`           | `guid`                            | No        | `00000000-0000-0000-0000-000000000000` | Aduana de procedencia. Solo se valida si es distinto de `Guid.Empty`. |
| `seal_number`                | `string?`                         | No        | `null`  | Marchamo / precinto. Si viene `null`, se conserva el valor actual. |
| `country_origin`             | `string?`                         | No        | `null`  | País de origen. Si viene `null`, se conserva el valor actual. |
| `container_number`           | `string?`                         | No        | `null`  | Número de contenedor. Si viene `null`, se conserva el valor actual. |
| `document_type`              | `enum (DocumentType)?`            | No        | `null`  | `DUCA` o `CustomsDeclaration`. Si se omite, se infiere del payload (presencia de `ducat_numbers` o `customs_declaration_number`); si no se puede inferir, se toma del valor actual de la recepción. |
| `ducat_numbers`              | `array (DucatNumbersUpdate)`      | No        | `[]`    | Lista de DUCAs a actualizar. Cada entrada debe incluir su `operational_order_id`. |
| `customs_declaration_number` | `string?`                         | No        | `null`  | Número de declaración aduanera. Solo aplica cuando `document_type = CustomsDeclaration`. |

### `ducat_numbers[]`

| Campo                  | Tipo     | Requerido | Descripción |
|------------------------|----------|:---------:|-------------|
| `operational_order_id` | `guid`   | Sí        | Id de la orden operativa asociada a esa DUCA. Debe pertenecer a la recepción. |
| `document_number`      | `string` | Sí        | Nuevo número de DUCA. |

> El `operational_order_id` se obtiene del `GET` de detalle, en `additional_data.document_numbers[].operational_order_id`.

---

## `reception_transport_information`

| Campo                    | Tipo                    | Requerido | Default | Descripción |
|--------------------------|-------------------------|:---------:|---------|-------------|
| `driver_name`            | `string?`               | No        | `null`  | Nombre del conductor. Si viene `null`, se conserva el actual. |
| `driver_license`         | `string?`               | No        | `null`  | Licencia del conductor. Si viene `null`, se conserva la actual. |
| `transportista`          | `string?`               | No        | `null`  | Transportista. Si viene `null`, se conserva el actual. |
| `vehicle_plate_number`   | `string?`               | No        | `null`  | Placa del vehículo. Si viene `null`, se conserva la actual. |
| `vehicle_chassis_number` | `string?`               | No        | `null`  | Número de chasis. Si viene `null`, se conserva el actual. |
| `transport_unit`         | `enum (TransportUnit)?` | No        | `null`  | Unidad de transporte. Si viene `null`, se conserva la actual. |

---

## `evidence_base64` y `evidence_ids_to_delete`

| Campo                   | Tipo            | Requerido | Default | Descripción |
|-------------------------|-----------------|:---------:|---------|-------------|
| `evidence_base64`       | `array (string)`| No        | `[]`    | Nuevas evidencias fotográficas en Base64. Se suben a S3 y se agregan al `additional_data`. |
| `evidence_ids_to_delete`| `array (guid)`  | No        | `[]`    | Ids (`image_id`) de evidencias existentes a eliminar. Se leen de `additional_data.evidence_urls[].image_id`. |

---

## Flujo del Handler (`UpdateReceptionEntranceHandler`)

1. `ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct)`. Si falla, responde `400`.
2. Valida que el rol del usuario sea `Administrator`, `Supervisor` o `Manager`. Si no, responde `403` (`ERP:INVALID_ACCESS`).
3. Carga la recepción con `Include(OperationalOrders)` filtrando por `Id` e `IsActive = true`. Si no existe: `ERP:NOT_FOUND_RECEPTION`.
4. Valida la ventana de edición: si pasaron **≥ 10 minutos** desde `CreatedAt` y el rol no es `Administrator` o `Manager`, responde `ERP:RECEPTION_UPDATE_TIME_EXPIRED`.
5. Deserializa `additional_data` a `AdditionalReceptionEntranceData`.
6. Si viene `general_information`, llama a `SyncDocuments(...)`, que:
   - Valida que no se mezclen `ducat_numbers` con `customs_declaration_number` (`ERP:CONFLICTING_DOCUMENT_FIELDS`).
   - Determina el `DocumentType` efectivo (payload → inferido → actual).
   - **Rama DUCA:** valida duplicados de número (`ERP:DUPLICATE_DOCUMENT_NUMBERS`) y de `operational_order_id` (`ERP:DUPLICATE_OPERATIONAL_ORDER_IDS`); verifica que cada `operational_order_id` pertenezca a la recepción (`ERP:INVALID_OPERATIONAL_ORDER_ID`); actualiza `OperationalOrder.DocumentNumber` y sincroniza `additional_data.document_numbers` por `operational_order_id`; recalcula `IsConsolidated` en todas las OPs.
   - **Rama CustomsDeclaration:** valida que exista exactamente 1 OP (`ERP:DOCUMENT_COUNT_MISMATCH`); actualiza `DocumentNumber` y sincroniza `additional_data` por `operational_order_id`, con fallback por tipo si el `additional_data` viene de un flujo viejo.
7. Aplica los campos generales (`SealNumber`, `CountryOfOrigin`, `ContainerNumber`) y, si `custom_branch_id != Guid.Empty`, valida la aduana y la asigna (`ERP:ERROR_CUSTOM_BRANCH` si no existe).
8. Si hay evidencias nuevas o ids a eliminar, sube las nuevas a S3 y elimina las indicadas del `additional_data`.
9. Serializa `additional_data` con `JsonNamingPolicy.SnakeCaseLower`, actualiza la recepción.
10. Si viene `reception_transport_information`, carga el `ReceptionTransportEntrance` por `ReceptionEntranceId` y aplica los campos no nulos (`DriverName`, `Transportista`, `DriverLicense`, `VehiclePlateNumber`, `VehicleChassisNumber`, `TransportUnit`). Si no existe: `ERP:ERROR_UPDATED`.
11. `SaveChangesAsync`.

> **Validación (FluentValidation):** el `UpdateReceptionEntranceValidator` está **vacío**. Solo se ejecutan las reglas de `BaseRequestValidator` (`CompanyId`, `ModuleCode`, `UserId`). Ningún campo del body se valida.

> **Operación por ID:** la actualización de documentos ya no se infiere por comparación de strings. Cada DUCA viaja con su `operational_order_id`, por lo que es posible actualizar N DUCAs en una sola llamada sin ambigüedad. No se permite agregar ni eliminar documentos por este endpoint: solo modificar los que ya existen.

> **Ventana de edición:** los roles `Administrator` y `Manager` pueden editar la información vehicular sin límite de tiempo. El resto solo dentro de los primeros 10 minutos.

---

## Respuestas

### ✅ 200 OK

La recepción se actualizó correctamente. El cuerpo de la respuesta va vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Sincronización de documentos | Cada `ducat_numbers[].operational_order_id` actualiza la `OperationalOrder` correspondiente y su entrada en `additional_data.document_numbers`. |
| Actualización parcial | Solo se actualizan las OPs enviadas en `ducat_numbers`. Las demás quedan intactas. |
| `document_type` | Si se omite, se infiere de `ducat_numbers` / `customs_declaration_number`. Si no se puede inferir ni existe valor actual, responde `ERP:INVALID_DOCUMENT_TYPE`. |
| Evidencias | Las nuevas se suben a S3 con `image_id` nuevo; las indicadas en `evidence_ids_to_delete` se eliminan del `additional_data`. |
| `custom_branch_id` | Si viene `Guid.Empty` se ignora (no se sobrescribe el valor actual). |
| Campos de transporte | Se aplican solo si vienen no nulos (merge parcial). |
| `IsConsolidated` | Se recalcula: `true` si la recepción tiene más de 1 OP, `false` en caso contrario. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:INVALID_OPERATIONAL_ORDER_ID",
    "description": "Uno o más OperationalOrderId no pertenecen a esta recepción"
  },
  "createdAt": "2026-10-05 10:15:32"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:NOT_FOUND_RECEPTION` | `La reception a actualizar no existe registrada` |
| `ERP:RECEPTION_UPDATE_TIME_EXPIRED` | `Ya no se puede modificar la información vehicular de la recepción` |
| `ERP:CONFLICTING_DOCUMENT_FIELDS` | `Envíe los números de DUCA o el número de declaración aduanera, no ambos` |
| `ERP:INVALID_DOCUMENT_TYPE` | `Tipo de documento no válido` / `No fue posible determinar el tipo de documento de la recepción` |
| `ERP:DUPLICATE_DOCUMENT_NUMBERS` | `La lista de números de DUCA contiene valores duplicados` |
| `ERP:DUPLICATE_OPERATIONAL_ORDER_IDS` | `La lista contiene OperationalOrderId duplicados` |
| `ERP:INVALID_OPERATIONAL_ORDER_ID` | `Uno o más OperationalOrderId no pertenecen a esta recepción` |
| `ERP:DOCUMENT_COUNT_MISMATCH` | `Una recepción con declaración aduanera debe tener exactamente una orden operativa` |
| `ERP:ERROR_CUSTOM_BRANCH` | `La aduana de procendencia que desea actualizar no esta registrada` |
| `ERP:ERROR_UPDATED` | `La información de transporte no se ha encontrado` |
| `Validation_Error` | Errores de `BaseRequestValidator` (`CompanyId`, `ModuleCode`, `UserId`). |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `ERP:INVALID_ACCESS` | `No tienes acceso a realizar esta acción`. El rol no es `Administrator`, `Supervisor` ni `Manager`. |
| `Forbidden` | `X-Api-Key` ausente o incorrecta. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** (fuera de `CoreException`) el `ExceptionMiddleware` responde en **PascalCase**:

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "Server_Error",
    "Description": "Error interno no controlado."
  },
  "CreatedAt": "2026-10-05 10:15:32"
}
```

---

## Códigos de Estado

| Código | Descripción |
|--------|-------------|
| `200` | Recepción actualizada exitosamente. |
| `400` | Error de validación, de acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | Rol sin permiso para la acción, o `X-Api-Key` inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `DocumentType`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain` y sus valores enteros no son legibles desde este repositorio, por lo que **no se documentan números**. Los únicos miembros aceptados por el handler son `DUCA` y `CustomsDeclaration`.

### `TransportUnit`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain`. Solo se usa en `reception_transport_information.transport_unit`.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details` | Obtiene el detalle de la recepción, incluyendo `additional_data.document_numbers[].operational_order_id` y `additional_data.evidence_urls[].image_id`. Es la fuente de los IDs que se reenvían en `ducat_numbers` y `evidence_ids_to_delete`. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Listado paginado de recepciones. |
| `POST /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Registra una nueva recepción. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/custom-branches` | Listado de aduanas, para validar `custom_branch_id`. |