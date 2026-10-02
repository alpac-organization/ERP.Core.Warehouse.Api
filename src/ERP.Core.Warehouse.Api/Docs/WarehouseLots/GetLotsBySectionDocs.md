# Tramos

## Listar Tramos

Endpoint para listar con paginación y filtros los tramos registrados dentro de una sección de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots` |
| **Descripción** | Lista los tramos de la sección, ordenados por código, con filtros opcionales por código y estado. |
| **Tags**        | `Tramos` |

---

## Parámetros de Ruta (Path Params)

| Parámetro       | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|:---------:|-------------|
| `company_id`    | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`   | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id`  | `guid`   | Sí        | Identificador único del almacén. |
| `sections_id`   | `guid`   | Sí        | Identificador único de la sección. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `X-Api-Key`     | `{api_key}`      | Sí        |

---

## Parámetros de Query

| Parámetro     | Tipo               | Requerido | Default | Descripción |
|---------------|--------------------|:---------:|---------|-------------|
| `code`        | `string`           | No        | `null`  | Coincidencia parcial sobre el código del tramo. |
| `status`      | `enum (RackStatus)`| No        | `null`  | Filtra por estado exacto del tramo. |
| `page_number` | `int`              | No        | `1`     | Número de página. |
| `page_size`   | `int`              | No        | `10`    | Registros por página. |

---

## Flujo del Handler (`GetLotsBySectionHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Busca la sección con `Id == sections_id AND IsActive AND DeletedAt == null`. Si no existe: `ERP:SECTION_NOT_FOUND`.
3. Verifica que `section.WarehouseId == warehouse_id`. Si no: `ERP:SECTION_WAREHOUSE_MISMATCH`.
4. Arma el query base: `Lots WHERE SectionId == sections_id AND DeletedAt == null`, `AsNoTracking`.
5. Si viene `code`, aplica `Trim()`, lo pasa a minúsculas y filtra con `Contains(..., CurrentCultureIgnoreCase)`.
6. Si viene `status`, filtra por igualdad exacta.
7. Cuenta el total, pagina con `Skip`/`Take` y ordena por `Code` ascendente.
8. Mapea con `LotsProfile` a `LotListItemDto` y devuelve el `PagedResponse`.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y `warehouse_id`, `sections_id`, `code`, `status`, `page_number` y `page_size` los asigna el action.

> **Validación (FluentValidation):** el `GetLotsBySectionValidator` valida `warehouse_id` requerido, `sections_id` requerido y distinto de `Guid.Empty`, y que `status` sea un valor del enum cuando viene informado.

> El listado **no** filtra por `IsActive` del tramo, solo por `DeletedAt == null`. Los tramos dados de baja por `DELETE` desaparecen del listado, pero los que tengan `IsActive = false` sin `DeletedAt` seguirían apareciendo.

---

## Respuestas

### ✅ 200 OK

Devuelve un `PagedResponse<LotListItemDto>`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`). Las fechas viajan en ISO 8601.

```json
{
  "data": [
    {
      "id": "56487c1b-9f4d-4b2a-8e1c-1234567890ab",
      "code": "A-01",
      "status": "Available",
      "allows_stacking": true,
      "unavailable_reason": null,
      "status_changed_at": "2026-09-09T10:00:00"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

| Campo                | Tipo              | Descripción |
|----------------------|-------------------|-------------|
| `id`                 | `guid`            | Id del tramo. Es el valor que consumen el `PATCH` y el `DELETE`. |
| `code`               | `string`          | Código del tramo. |
| `status`             | `enum (RackStatus)` | Estado del tramo. |
| `allows_stacking`    | `bool`            | Si admite apilamiento. |
| `unavailable_reason` | `string`          | Motivo de no disponibilidad. `null` si está disponible. |
| `status_changed_at`  | `datetime`        | Última vez que cambió el estado. `null` si nunca cambió. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Orden | Por `code` ascendente. |
| Filtro de código | Parcial, no exacto. Un código con espacios al inicio o final se normaliza con `Trim()` antes de comparar. |
| Sin filtro de almacén | La pertenencia del almacén se valida a través de la sección, no sobre el tramo. |
| Capacidad | El listado no devuelve `lots_capacity`. Para obtenerla hay que llamar al endpoint de capacidades. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:SECTION_NOT_FOUND",
    "description": "La sección indicada no existe o no está activa."
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | `La sección no pertenece al almacén indicado.` |
| `Validation_Error` | Errores del `GetLotsBySectionValidator`: `El id del almacén es requerido.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `El estado del tramo no es válido.` |
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
| `200` | Listado obtenido exitosamente. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `RackStatus`

| Valor | Nombre            | Descripción |
|:-----:|-------------------|-------------|
| `1`   | `Available`       | Listo y libre para asignar mercadería. |
| `2`   | `Occupied`        | Tiene mercadería asignada actualmente. |
| `3`   | `UnderMaintenance`| Fuera de servicio por mantenimiento. |
| `4`   | `Blocked`         | Inhabilitado por otra causa. |
| `5`   | `Reserved`        | Apartado para una operación en curso. |