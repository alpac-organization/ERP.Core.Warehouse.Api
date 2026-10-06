# Órdenes Operacionales

## Registrar Información de Recepción

Endpoint para registrar y actualizar la información de recepción de una orden operacional: cliente, peso, bultos, datos de transporte (`shipping_company`, `consignee`, `sender`) y, opcionalmente, la lista de mercaderías contenidas.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/information` |
| **Descripción** | Asigna el cliente (si se envía), actualiza peso, bultos y datos de transporte, y agrega las mercaderías indicadas a la orden operacional. |
| **Tags**        | `Solicitudes de compras` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `operational_order_id` | `guid`   | Sí        | Identificador único de la orden operacional. |

> El controller **sobrescribe** el `OperationalOrderId` del body con el de la ruta (`payload.OperationalOrderId = operational_order_id`). Si envías ambos y difiere, gana el de la ruta.

---

## Headers

| Header          | Valor                         | Requerido |
|-----------------|-------------------------------|-----------|
| `Authorization` | `Bearer {token}`              | Sí        |
| `X-Api-Key`     | `{api_key}`                   | Sí        |
| `Content-Type`  | `application/json`            | Sí        |

---

## Body

```json
{
  "customer_id": "8d8c47fd-337c-41b7-8156-eac944027ff7",
  "package_amount": 42,
  "merchandise_weight": 1250.50,
  "shipping_company": "Transportes del Pacífico",
  "consignee": "RECEPTOR FINAL",
  "sender": "EXPORTADORA XYZ",
  "merchandises": [
    {
      "merchandise": "Acero inoxidable",
      "merchandise_description": "Bobinas de acero inoxidable AISI 304"
    },
    {
      "merchandise": "Aluminio",
      "merchandise_description": null
    }
  ]
}
```

| Campo                | Tipo                    | Requerido | Default | Descripción |
|----------------------|-------------------------|:---------:|---------|-------------|
| `customer_id`        | `guid?`                 | No        | `null`  | Cliente al que se asigna la orden. **Opcional:** solo se procesa si viene informado y no es `Guid.Empty`. Debe existir y estar **activo**. |
| `package_amount`     | `decimal?`              | No        | `null`  | Cantidad de bultos. Se escribe en `OperationalOrder.PackagesCount`. |
| `merchandise_weight` | `decimal?`              | No        | `null`  | Peso de la mercadería. Se escribe en `OperationalOrder.Weight`. |
| `shipping_company`   | `string?`               | No        | `null`  | Transportista. Se escribe en `OperationalOrder.ShippingCompany`. |
| `consignee`          | `string?`               | No        | `null`  | Consignatario. Se escribe en `OperationalOrder.Consignee`. |
| `sender`             | `string?`               | No        | `null`  | Remitente. Se escribe en `OperationalOrder.Sender`. |
| `merchandises`       | `array<MerchandiseCreate>?` | No    | `null`  | Lista de mercaderías. Cada llamada **agrega** nuevas asignaciones; no reemplaza las existentes. |

| Campo de cada item de `merchandises` | Tipo     | Descripción |
|---------------------------------------|----------|-------------|
| `merchandise`                         | `string?` | Tipo de mercadería. Se persiste con `Trim()`. |
| `merchandise_description`             | `string?` | Descripción de la mercadería. Se persiste con `Trim()`. |

> Un item se considera válido si **al menos uno** de los dos campos tiene contenido no vacío. Los items donde ambos son `null` o en blanco se descartan **silenciosamente** (sin error).

---

## Flujo del Handler (`ReceptionInformationOperationalHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Busca la orden operacional con `Where(Id == operational_order_id).FirstOrDefaultAsync()`, **sin ningún `Include`**. Si no existe devuelve `404` con `ERP:01`.
3. Calcula `customerChanged = CustomerId.HasValue && CustomerId != Guid.Empty`.
4. Si la orden **no tiene cliente** (`CustomerId == null`) y no se envió uno nuevo, devuelve `400` con `ERP:CUSTOMER_REQUIRED`.
5. Si `customerChanged`, busca el cliente con `IsActive && Id == customer_id`. Si no existe devuelve `404` con `ERP:04`; si existe, asigna `operationalOrder.CustomerId`. Si `customerChanged` es `false`, **se conserva el cliente actual**.
6. Actualiza los escalares con semántica de **omisión**: `Weight = merchandise_weight ?? Weight`, `PackagesCount = package_amount ?? PackagesCount`, `ShippingCompany`, `Consignee` y `Sender` de la misma forma (`??` con el valor actual).
7. Filtra `merchandises` descartando los items sin contenido. Si queda al menos uno:
   7.1 Detecta duplicados agrupando por `Merchandise.Trim()` con `StringComparer.OrdinalIgnoreCase`. Si hay duplicados devuelve `400` con `ERP:DUPLICATE_MERCHANDISE`.
   7.2 Crea una `AssignmentOperational` por item: `Id = Guid.NewGuid()`, `IsActive = true`, `OperationalOrderId`, `Merchandise`/`MerchandiseDescription` (con `Trim()`), `HasMerchandiseDescription = !IsNullOrWhiteSpace(description)`, `Status = AssignmentOperationalStatus.None`, `Category = MerchandiseCategory.None`, `DestinationType = DestinationType.None`, `AdditionalData = "{}"`.
   7.3 Marca la orden con `HasAssignmentOperationalActive = true` y `Status = OperationalOrderStatus.Assignment`.
8. Marca la orden con `UpdateAsync` y persiste con `SaveChangesAsync`. Devuelve `200 OK` sin body.

> **Contexto de la request:** `company_id`, `module_code` y `operational_order_id` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y el resto del body se deserializa al `ReceptionInformationOperationalCommand`.

> **Validación (FluentValidation):** `ReceptionInformationOperationalValidator` valida las reglas de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`), más: `OperationalOrderId` no vacío ni `Guid.Empty`; `MerchandiseWeight > 0` **solo si viene informado**; `PackageAmount > 0` **solo si viene informado**. **No** se valida `customer_id`, `shipping_company`, `consignee`, `sender` ni ningún campo de `merchandises`.

> **La regla de `OperationalOrderId` es inalcanzable en la práctica:** el validator recibe el valor que el controller ya pisó con la ruta. Si la ruta trae un GUID mal formado, el binding del modelo falla antes y produce `400 Validation_Error` del framework.

> **Este endpoint no restringe por rol.** No hay ninguna comprobación de `Supervisor` ni de ningún otro `RoleType`. El detalle (`GET .../details`) sí bloquea al `Supervisor`, así que un supervisor puede escribir aquí información que luego no puede leer.

### ✅ Comportamiento actual: sí es un PATCH parcial

Todos los campos del body son opcionales y el handler conserva el valor actual con `??` cuando el campo viene `null`:

| Campo enviado | Valor que se persiste |
|---------------|------------------------|
| `package_amount` omitido / `null` | Se conserva `OperationalOrder.PackagesCount` actual. |
| `merchandise_weight` omitido / `null` | Se conserva `OperationalOrder.Weight` actual. |
| `shipping_company`, `consignee`, `sender` omitidos / `null` | Se conserva el valor actual de cada campo. |
| `customer_id` omitido, `null` o `Guid.Empty` | Se conserva el cliente actual. **Excepto:** si la orden no tiene cliente, responde `400 ERP:CUSTOMER_REQUIRED`. |
| `merchandises` omitido / `null` / `[]` | No se crean asignaciones y el estado de la orden no cambia. |

> `Guid.Empty` explícito (`"00000000-0000-0000-0000-000000000000"`) se trata igual que un `customer_id` omitido: no intenta cambiar de cliente.

### ⚠️ Comportamiento actual: las mercaderías son acumulativas

- **No existe** el chequeo de "la mercadería ya fue recepcionada" (`ERP:02` de versiones anteriores): se eliminó.
- Cada llamada con items válidos **agrega** nuevas `AssignmentOperational` a la orden; nunca reemplaza ni edita las existentes.
- El duplicado se evalúa **solo sobre `merchandise`** (no sobre la descripción), ignorando mayúsculas/minúsculas y espacios exteriores.
- Al agregar mercadería, la orden pasa de estado a `Assignment` y se marca `HasAssignmentOperationalActive = true`. Sin mercadería, el estado no se toca.

### ⚠️ Mensajes del código fuente

- `ERP:01` dice `Operational order with ID {id} not found.` — **en inglés**, a diferencia del resto de los mensajes del módulo, que están en español.

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
| Sin body | La respuesta es un `200` vacío. No hay eco de los valores persistidos. Para confirmarlos hay que consultar el detalle. |
| Cliente inactivo | Un cliente existente pero con `IsActive = false` produce el mismo `404` (`ERP:04`) que uno inexistente. |
| Asignación de mercadería | Al crear asignaciones la orden **sí cambia de estado** a `Assignment` y habilita `HasAssignmentOperationalActive`. |
| Sin mercadería | Solo se actualizan los escalares; la orden conserva su estado. |
| Rol | Sin restricción de rol. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:DUPLICATE_MERCHANDISE",
    "description": "La lista contiene mercancías duplicadas: Acero inoxidable"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:CUSTOMER_REQUIRED` | `Debe asignar un cliente a la orden operacional.` La orden no tiene cliente y no se envió `customer_id`. |
| `ERP:DUPLICATE_MERCHANDISE` | `La lista contiene mercancías duplicadas: {nombres}` Dos o más items comparten `merchandise` (ignorando mayúsculas y espacios). |
| `Validation_Error` | Errores de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`) o del propio validator: `El id de la orden operacional es requerido.`, `El id de la orden operacional no es válido.`, `El peso de la mercancía debe ser mayor que cero.`, `La cantidad de bultos debe ser mayor que cero.` |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. *(solo origina en `ValidateAccessAsync`; ya no se usa para mercadería)* |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 404 Not Found

```json
{
  "status": 404,
  "error": {
    "typeError": "ERP:01",
    "description": "Operational order with ID 56487c1b-9f4d-4b2a-8e1c-1234567890ab not found."
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:01` | `Operational order with ID {id} not found.` La orden operacional no existe. |
| `ERP:04` | `El cliente seleccionado no existe en nuestros registros.` El `customer_id` enviado no corresponde a un cliente **activo**. |

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
| `200` | Información registrada exitosamente. Sin body. |
| `400` | Error de validación, de acceso, cliente requerido o mercancías duplicadas (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `404` | La orden operacional (`ERP:01`) o el cliente (`ERP:04`) no existe / no está activo. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

> ⚠️ **Confirmar valores numéricos y la lista completa de miembros.** Todos los enums viven en el paquete NuGet `ERP.Core.Database.Domain` y no son legibles desde este repositorio, por lo que **no se documentan números**.

### `OperationalOrderStatus`

Este endpoint escribe `Assignment` sobre la orden cuando la request incluye mercaderías válidas. La orden se crea con `PendingDocument` (ver [registro de recepción](../RecepcionEntrance/CreateReceptionEntranceDocs.md)).

### `AssignmentOperationalStatus`

Cada mercadería se crea con `Status = None`. Contrasta con el registro de recepción, que crea las suyas con `Status = Pending`: **son estados distintos para entidades del mismo tipo**.

### `MerchandiseCategory` / `DestinationType`

Se crean con `None` en ambos casos; el PATCH no permite asignarlos.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/operational-orders` | Listado paginado. De ahí se obtiene el `operation_order_id`. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/details` | Detalle de la orden, para verificar lo registrado. Bloquea al rol `Supervisor`. |
| `POST /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Genera las órdenes operacionales a partir de una recepción. |
