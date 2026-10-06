# Órdenes Operacionales

## Listar Órdenes Operacionales

Endpoint para listar con paginación las órdenes operacionales (PO) registradas, con filtros opcionales por código de PO, CIF del cliente y estado.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders` |
| **Descripción** | Lista las órdenes operacionales ordenadas por fecha de creación (descendente). |
| **Tags**        | `Solicitudes de compras` |

---

## Parámetros de Ruta (Path Params)

| Parámetro     | Tipo     | Requerido | Descripción |
|:------------:|:--------:|:---------:|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `X-Api-Key`     | `{api_key}`      | Sí        |

---

## Parámetros de Query

| Parámetro       | Tipo                        | Requerido | Default | Descripción |
|-----------------|-----------------------------|:---------:|---------|-------------|
| `code`          | `string`                    | No        | `null`  | Coincidencia **exacta** sobre `PoCode`. |
| `customer_cif`  | `string`                    | No        | `null`  | Coincidencia **exacta** sobre `Customer.Cif`. |
| `document_type` | `enum (DocumentType)`       | No        | `null`  | **No se aplica.** El parámetro se recibe y se asigna al query, pero el handler nunca lo usa como filtro. |
| `status`        | `enum (OperationalOrderStatus)` | No    | `null`  | Filtra por estado exacto. |
| `page_number`   | `int`                       | No        | `1`     | Número de página. |
| `page_size`     | `int`                       | No        | `10`    | Registros por página. |

---

## Flujo del Handler (`GetOperationalOrdersHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Arma el query base: `OperationalOrders` con `Include(Customer)`, `Include(CostCenter)`, `AsSplitQuery()` y `AsNoTracking()`.
3. Si viene `status`, filtra por `Status == request.Status` (igualdad exacta).
4. Si viene `customer_cif`, filtra por `Customer.Cif == request.CustomerCif` (igualdad exacta).
5. Si viene `code`, filtra por `PoCode == request.PoCode` (igualdad exacta).
6. Cuenta el total, pagina con `Skip`/`Take` y ordena por `CreatedAt` descendente.
7. Mapea con `OperationalOrderProfile` a `OperationalOrderDto` y devuelve el `PagedResponse`.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y el resto de parámetros los asigna el action.

> **Validación (FluentValidation):** el `GetOperationalOrdersValidator` valida que `status` sea un valor del enum cuando viene informado, y que `customer_cif` y `code` no sean cadenas vacías cuando vienen informadas.

> **El listado no aplica ningún filtro de estado de la entidad:** no filtra por `IsActive` ni por `DeletedAt == null`. Toda orden operacional existente en la tabla entra en el listado, incluidas las dadas de baja o marcadas como inactivas.

> **El parámetro `document_type` es un filtro muerto:** el controller lo expone y el query lo transporta, pero el handler nunca lo consulta. Enviarlo no cambia el resultado.

> **Este endpoint no restringe por rol.** No hay comprobación de `Supervisor` ni de ningún otro `RoleType`; solo la validación de acceso de `ValidateAccessAsync`.

### ⚠️ Trampas de validación

| Situación | Resultado |
|---|---|
| `?customer_cif=` (cadena vacía) | `400 Validation_Error`. El validator usa `When(x => x.CustomerCif is not null)`, y una query string vacía deserializa a `""`, que no es `null`. |
| `?code=` (cadena vacía) | `400 Validation_Error` por la misma razón. |
| `?status=` (vacío) | No dispara validación porque no llega a ser un valor de enum; el binding lo deja en `null`. |
| `page_size` negativo | Lanza excepción no controlada al evaluar `Take(page_size)`, producing `500 Server_Error`. El validator no valida `page_number` ni `page_size`. |

---

## Respuestas

### ✅ 200 OK

Devuelve un `PagedResponse<OperationalOrderDto>`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`).

```json
{
  "data": [
    {
      "operation_order_id": "56487c1b-9f4d-4b2a-8e1c-1234567890ab",
      "po_code": "ALP-MGA-OP-15",
      "document_number": "DUCA-000123",
      "document_type": "DUCA",
      "status": "PendingDocument",
      "is_alerted": false
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

| Campo                     | Tipo                            | Descripción |
|---------------------------|---------------------------------|-------------|
| `operation_order_id`      | `guid`                          | Id de la orden operacional. Es el valor que consumen el detalle y el PATCH de información. |
| `po_code`                 | `string`                        | Código de la PO. |
| `document_number`         | `string`                        | Número de DUCA o de declaración aduanera asociado. |
| `document_type`           | `enum (DocumentType)`           | Tipo de documento de la orden. |
| `status`                  | `enum (OperationalOrderStatus)` | Estado de la orden. |
| `is_alerted`              | `bool`                          | Si la orden tiene una alerta activa. |

### Notas

| Campo / regla | Descripción |
|---|---|
| `operation_order_id` | **Typo en el código fuente.** La propiedad C# se llama `OperationOrderId` (sin la "al"), pero el nombre en el contrato JSON es `operation_order_id`. Se documenta tal cual viaja para no romper integraciones. |
| Orden | Por `created_at` descendente: las más recientes primero. |
| Filtro de `code` | Exacto, **no parcial**. `ALP-MGA-OP-15` no coincide con `ALP-MGA-OP-150`. |
| Filtro de `customer_cif` | Exacto sobre `Customer.Cif`. |
| Filtro de `document_type` | **Sin efecto.** Ver nota arriba. |
| Cliente / centro de costos | El handler sigue haciendo `Include(Customer)` (necesario para el filtro `customer_cif`) e `Include(CostCenter)`, pero **ninguno de los dos se expone en la respuesta**: el `OperationalOrderDto` ya no tiene `customer_information` ni `cost_center_information`. |
| `total` | Cuenta **después** de aplicar los filtros de `status`, `customer_cif` y `code`. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:INVALID_ACCESS",
    "description": "No tienes acceso a esta compañía o módulo"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `Validation_Error` | `El estado debe ser un valor de enum valido`, `El código de cliente no debe ser vacío.`, `El código de la PO de cliente no debe ser vacío.` |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

> `ERP:INVALID_ACCESS` no se emite en este endpoint: no hay ninguna restricción por rol en el handler.

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
| `200` | Listado obtenido exitosamente. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `DocumentType`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain` y sus valores enteros no son legibles desde este repositorio. Los docs de recepción manejan cifras distintas para el mismo enum, así que **no se documentan aquí números**. Este endpoint acepta el parámetro pero no lo aplica.

Los miembros referenciados en el código del módulo son `DUCA` y `CustomsDeclaration`.

### `OperationalOrderStatus`

> ⚠️ **Confirmar valores numéricos y la lista completa de miembros.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain`. Los miembros referenciados explícitamente en el código de este módulo son `PendingDocument` (estado inicial con el que se crea toda orden operacional desde el registro de recepción) y `Assignment` (al que pasa la orden cuando se le agrega mercadería vía PATCH de información). También aparece `Completed` en el módulo de ServiceOrders.

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/details` | Detalle de una orden operacional. |
| `PATCH /api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/information` | Registra la información de recepción (cliente, peso, bultos, transporte y mercadería) de la orden. |
| `POST /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Genera las órdenes operacionales a partir de una recepción. |