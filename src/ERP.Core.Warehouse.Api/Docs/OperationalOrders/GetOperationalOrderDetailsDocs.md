# Órdenes Operacionales

## Obtener Detalle de Orden Operacional

Endpoint para obtener el detalle completo de una orden operacional, incluyendo la información de recepción asociada si la orden ya fue recepcionada.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/details` |
| **Descripción** | Devuelve el detalle de la orden operacional indicada, con cliente, centro de costos, datos de transporte y recepción. |
| **Tags**        | `Solicitudes de compras` |

---

## Parámetros de Ruta (Path Params)

| Parámetro             | Tipo     | Requerido | Descripción |
|:---------------------:|:--------:|:---------:|-------------|
| `company_id`          | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`         | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `operational_order_id` | `guid`  | Sí        | Identificador único de la orden operacional. Es el valor que devuelve `operation_order_id` en el listado. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `X-Api-Key`     | `{api_key}`      | Sí        |

---

## Flujo del Handler (`GetOperationalOrderDetailsHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Si el rol del usuario es `Supervisor`, devuelve `400` con `ERP:INVALID_ACCESS`.
3. Arma el query base sobre `OperationalOrders` con `Include(Customer)`, `Include(Reception).ThenInclude(ReceptionTransport)` e `Include(CostCenter)`, filtrando por `Id == operational_order_id`.
4. Obtiene la primera coincidencia con `FirstOrDefaultAsync`.
5. Mapea con `OperationalOrderProfile` a `OperationalOrderDetailsDto` y lo devuelve.

> **Contexto de la request:** `company_id`, `module_code` y `operational_order_id` llegan por la ruta, y `user_id` se lee de `HttpContext.Items["UserId"]`.

> **Validación (FluentValidation):** el `GetOperationalOrderDetailsValidator` declara `OperationalOrderId` con `NotNull()`. **Esta regla nunca puede fallar**, porque `OperationalOrderId` es un `Guid` no nulable: si el cliente envía un id mal formado o vacío, el binding del modelo falla antes y produce `400 Validation_Error` del framework. El mensaje `El id de la orden operacional es obligatorio` es inalcanzable.

> **El query es *tracked*:** a diferencia del listado, este handler **no** aplica `AsNoTracking()` ni `AsSplitQuery()`. El orden no cambia el estado de la base de datos, pero la entidad queda adjunta al contexto durante el request.

> ⚠️ **El handler no valida que la orden exista.** Si el `operational_order_id` no corresponde a ninguna orden, `_mapper.Map<OperationalOrderDetailsDto>(null)` devuelve `null` y el endpoint responde **`200 OK` con el body `null`** en lugar de un `404`. Verifícalo siempre en el cliente antes de deserializar.

---

## Respuestas

### ✅ 200 OK

Devuelve un `OperationalOrderDetailsDto`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`).

```json
{
  "operation_order_id": "56487c1b-9f4d-4b2a-8e1c-1234567890ab",
  "po_code": "ALP-MGA-OP-15",
  "document_number": "DUCA-000123",
  "document_type": "DUCA",
  "status": "PendingDocument",
  "is_alerted": false,
  "description": "Importación de mercadería general",
  "policy_number": "POL-99887",
  "shipping_company": "Transportes del Pacífico",
  "consignee": "RECEPTOR FINAL",
  "sender": "EXPORTADORA XYZ",
  "weight": 1250.50,
  "packages_count": 42,
  "customer_information": {
    "customer_id": "8d8c47fd-337c-41b7-8156-eac944027ff7",
    "cif": "20123456789",
    "customer_name": "CLIENTE DE PRUEBA"
  },
  "cost_center_information": {
    "cost_center_id": "f006b2d8-af3f-4c8b-b59c-a08b677b66ca",
    "description": null,
    "cost_center_name": "GERENCIA DE INFORMATICA",
    "coil_code": 4,
    "cost_center_code": 0
  },
  "reception_entrance_information": {
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
    "additional_data": "{\"document_numbers\":[{\"document_id\":\"7c2e9a11-5b3d-4c8e-9f01-2a3b4c5d6e7f\",\"document_numbers\":\"DUCA-000123\",\"document_type\":1}],\"evidence_urls\":[]}",
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
}
```

| Campo                             | Tipo                             | Descripción |
|-----------------------------------|----------------------------------|-------------|
| `operation_order_id`              | `guid`                           | Id de la orden operacional. |
| `po_code`                         | `string`                         | Código de la PO. |
| `document_number`                 | `string`                         | Número de DUCA o de declaración aduanera. |
| `document_type`                   | `enum (DocumentType)`            | Tipo de documento de la orden. |
| `status`                          | `enum (OperationalOrderStatus)`  | Estado de la orden. |
| `is_alerted`                      | `bool`                           | Si la orden tiene una alerta activa. |
| `description`                     | `string`                         | Descripción de la orden. |
| `policy_number`                   | `string`                         | Número de póliza. |
| `shipping_company`                | `string`                         | Transportista asociado a la orden. |
| `consignee`                       | `string`                         | Consignatario. |
| `sender`                          | `string`                         | Remitente. |
| `weight`                          | `decimal`                        | Peso total de la mercadería. |
| `packages_count`                  | `decimal`                        | Cantidad de bultos. |
| `customer_information`            | `object (CustomerInformation)`   | Cliente asociado. `null` si la orden no tiene cliente. |
| `cost_center_information`         | `object (CostCenterInformation)` | Centro de costos asociado. `null` si la orden no tiene centro de costos. |
| `reception_entrance_information`  | `object (ReceptionEntranceDetailsDto)` | Recepción asociada. **`null` si la orden aún no ha sido recepcionada.** |

> `reception_entrance_information` es el mismo `ReceptionEntranceDetailsDto` que devuelve el detalle de recepción, y por lo tanto tiene la misma forma y los mismos campos. Consulta su [documentación](../RecepcionEntrance/GetReceptionEntranceDetailsDocs.md).

### Notas

| Campo / regla | Descripción |
|---|---|
| `operation_order_id` | **Typo en el código fuente.** La propiedad C# se llama `OperationOrderId` (sin la "al"). El nombre en el contrato JSON es `operation_order_id`. |
| `document_type` | Vive en el DTO base (`OperationalOrderDto`), por lo que también aparece en el listado. |
| Receptoría anidada | Es el único endpoint que expone la recepción **dentro** de la orden operacional. El listado no la incluye. |
| Centro de costos | El detalle **sí expone** `cost_center_information`: el profile lo mapea desde `CostCenter` y el handler lo trae con `Include(CostCenter)`, por lo que viene poblado. El listado, en cambio, no lo expone. |
| Orden no encontrada | Devuelve `200` con `null`, no `404`. |
| Rol `Supervisor` | Bloqueado con `400` / `ERP:INVALID_ACCESS`. |
| `additional_data` | Llega como **string JSON serializado**, no como objeto. Hay que hacer doble deserialización para leer `document_numbers` y `evidence_urls`. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:INVALID_ACCESS",
    "description": "No tienes acceso para verificar esta información"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:INVALID_ACCESS` | `No tienes acceso para verificar esta información`. Se emite únicamente cuando el rol del usuario es `Supervisor`. |
| `Validation_Error` | Errores de binding del modelo si `operational_order_id` no es un GUID válido. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

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
| `200` | Detalle obtenido. Si el id no existe, también `200` pero con el body `null`. |
| `400` | Error de validación, acceso o rol `Supervisor` (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

> **No existe `404`.** A diferencia del PATCH de información, este endpoint nunca devuelve `404` aunque la orden no exista.

---

## Catálogos de Enums Utilizados

### `DocumentType`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain` y sus valores enteros no son legibles desde este repositorio, por lo que **no se documentan números**. Los miembros referenciados en el código del módulo son `DUCA` y `CustomsDeclaration`.

### `OperationalOrderStatus`

> ⚠️ **Confirmar valores numéricos y la lista completa de miembros.** Los miembros referenciados en el código son `PendingDocument` (estado inicial de toda orden) y `Assignment` (estado al que pasa cuando se le agrega mercadería vía PATCH de información). También aparece `Completed` en el módulo de ServiceOrders.

### `TransportUnit`

> ⚠️ **Confirmar valores numéricos.** Aparece únicamente dentro de `reception_entrance_information.reception_transport_entrance_information.transport_unit`.

### `RoleType`

> ⚠️ **Confirmar valores numéricos.** Solo se usa la comparación contra `Supervisor`. También `Administrator` y `Manager` aparecen en el PATCH de recepción.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/operational-orders` | Listado paginado de órdenes operacionales. De ahí se obtiene el `operation_order_id`. |
| `PATCH /api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/information` | Registra la información de recepción de la orden. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details` | Detalle de la recepción, con la forma exacta de `reception_entrance_information`. |