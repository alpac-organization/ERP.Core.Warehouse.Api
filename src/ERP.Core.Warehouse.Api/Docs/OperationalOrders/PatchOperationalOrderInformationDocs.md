# Órdenes Operacionales

## Registrar Información de Recepción

Endpoint para registrar y actualizar la información de recepción de una orden operacional: cliente, peso, cantidad de bultos y, opcionalmente, la mercadería contenida.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/information` |
| **Descripción** | Asigna el cliente, el peso y los bultos a la orden operacional, y registra la asignación de mercadería cuando corresponde. |
| **Tags**        | `Solicitudes de compras` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `operational_order_id` | `guid`   | Sí        | Identificador único de la orden operacional. |

> El controller **sobrescribe** el `operational_order_id` del body con el de la ruta. Si envías ambos y difieren, gana el de la ruta.

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
  "has_merchandise": true,
  "merchandise_information": {
    "merchandise": "Acero inoxidable",
    "merchandise_description": "Bobinas de acero inoxidable AISI 304"
  }
}
```

| Campo                      | Tipo                          | Requerido | Default | Descripción |
|----------------------------|-------------------------------|:---------:|---------|-------------|
| `customer_id`              | `guid`                        | Sí        | —       | Cliente al que se asigna la orden. Debe existir y estar **activo**. |
| `package_amount`           | `decimal`                     | Sí        | `0`     | Cantidad de bultos. Se escribe en `OperationalOrder.PackagesCount`. |
| `merchandise_weight`       | `decimal`                     | Sí        | `0`     | Peso de la mercadería. Se escribe en `OperationalOrder.Weight`. |
| `has_merchandise`          | `bool`                        | No        | `false` | Si es `true`, registra una asignación de mercadería para la orden. |
| `merchandise_information`  | `object (MerchandiseInformation)` | No    | `null`  | Datos de la mercadería. Solo se lee si `has_merchandise` es `true`. |

| Campo de `merchandise_information` | Tipo     | Descripción |
|-------------------------------------|----------|-------------|
| `merchandise`                       | `string` | Tipo de mercadería. |
| `merchandise_description`           | `string` | Descripción de la mercadería. |

---

## Flujo del Handler (`ReceptionInformationOperationalHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Busca la orden operacional con `Include(Customer)`, `Include(CostCenter)` e `Include(HasAssignmentOperationalActive)`, filtrando por `Id == operational_order_id`. Si no existe devuelve `404`.
3. Busca el cliente con `Id == customer_id AND IsActive`. Si no existe devuelve `404`.
4. Sobrescribe la orden: `CustomerId`, `Weight = merchandise_weight` y `PackagesCount = package_amount`.
5. Marca la orden para actualización con `UpdateAsync`.
6. Si `has_merchandise` es `true` **y** la orden ya tiene `HasAssignmentOperationalActive`, devuelve `400`.
7. Si `has_merchandise` es `true`, registra una asignación de mercadería con `Status = None`, `Merchandise`, `MerchandiseDescription` y `HasMerchandiseDescription = true`.
8. Guarda los cambios con `SaveChangesAsync` y devuelve `200 OK` sin body.

> **Contexto de la request:** `company_id`, `module_code` y `operational_order_id` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y el resto del body se deserializa al `ReceptionInformationOperationalCommand`.

> **Validación (FluentValidation):** el `ReceptionInformationOperationalValidator` está **vacío**. Solo se ejecutan las reglas de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`). **Ninguno** de los campos del body se valida: se puede enviar `customer_id` con `Guid.Empty`, `package_amount` negativo o `has_merchandise` en `true` sin `merchandise_information`.

> **Este endpoint no restringe por rol.** No hay ninguna comprobación de `Supervisor` ni de ningún otro `RoleType`. El detalle (`GET .../details`) sí bloquea al `Supervisor`, así que un supervisor puede escribir aquí información que luego no puede leer.

### ⚠️ Comportamiento actual: no es un PATCH parcial

Aunque el verbo sea `PATCH`, el handler **siempre sobrescribe** los tres campos. Cada campo omitted llega con el valor por defecto de C# y se escribe igual:

| Campo enviado | Valor que se persiste |
|---------------|------------------------|
| `package_amount` omitido | `0` |
| `merchandise_weight` omitido | `0` |
| `customer_id` omitido | `Guid.Empty` → `404`, porque no habrá cliente activo con ese id |

Para conservar el valor actual hay que **reenviar explícitamente** el dato que se quiere mantener.

### ⚠️ Comportamiento actual: orden de las validaciones

El handler llama `UpdateAsync` (paso 5) **antes** de comprobar si la mercadería ya fue recepcionada (paso 6). Si el paso 6 devuelve `400`, el `SaveChangesAsync` final nunca se ejecuta, así que los cambios del paso 4 **no se persisten**. La respuesta `400` no deja datos a medias, pero el orden es frágil ante futures refactorizaciones.

### ⚠️ Comportamiento actual: mensajes inconsistentes

- `ERP:02` dice `La información de la mercaderia ya ha sido recepciponada.` — con el typo **"recepciponada"** en el código fuente, y sin tildes.
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
| Cliente inactivo | Un cliente existente pero con `IsActive = false` produce el mismo `404` que uno inexistente. |
| Asignación de mercadería | Crear una asignación **no** cambia el estado de la orden ni habilita la recepción de la asignación en este endpoint. |
| `has_merchandise` en `false` | No se crea ninguna asignación, pero `weight` y `packages_count` sí se actualizan. |
| Rol | Sin restricción de rol. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:02",
    "description": "La información de la mercaderia ya ha sido recepciponada."
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:02` | `La información de la mercaderia ya ha sido recepciponada.` Se emite cuando `has_merchandise` es `true` y la orden ya tiene `HasAssignmentOperationalActive = true`. |
| `Validation_Error` | Errores de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`) o de binding del body. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. *(mismo código, distinto origen: `ValidateAccessAsync`)* |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

> ⚠️ **Colisión de código `ERP:02`.** El mismo `typeError` `ERP:02` se usa para dos condiciones distintas: "mercadería ya recepcionada" (handler) y "usuario bloqueado temporalmente" (`ValidateAccessAsync`). Hay que leer el `description` para saber cuál ocurrió.

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
| `ERP:04` | `El cliente seleccionado no existe en nuestros registros.` El `customer_id` no corresponde a un cliente **activo**. |

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
| `400` | Error de validación, de acceso, o mercadería ya recepcionada (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `404` | La orden operacional (`ERP:01`) o el cliente (`ERP:04`) no existe / no está activo. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `AssignmentOperationalStatus`

> ⚠️ **Confirmar valores numéricos y la lista completa de miembros.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain`.

Este endpoint crea la asignación de mercadería con `Status = AssignmentOperationalStatus.None`. Contrasta con el registro de recepción, que crea la suya con `Status = AssignmentOperationalStatus.Pending`: **son estados distintos para entidades del mismo tipo**.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/operational-orders` | Listado paginado. De ahí se obtiene el `operation_order_id`. |
| `GET /api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/details` | Detalle de la orden, para verificar lo registrado. Bloquea al rol `Supervisor`. |
| `POST /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Genera las órdenes operacionales a partir de una recepción. |