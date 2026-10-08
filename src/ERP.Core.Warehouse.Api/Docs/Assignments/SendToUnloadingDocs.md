# Asignaciones Operativas

## Enviar a Bodega

Endpoint para marcar una asignación operativa como enviada a descargar. Cambia el estado de la asignación a `Pending`, habilitando el flujo de descarga (`assign-positions`, inicio de descarga, etc.).

> Como requisito, la asignación debe tener colaboradores y maquinaria asignados y tener registrada una observación/instrucción.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/send-to-unloading` |
| **Descripción** | Marca la asignación operativa como enviada a descargar (estado `Pending`). |
| **Tags**        | `Enviar a Bodega` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo dentro de la compañía (ej. `WAREHOUSE`). |
| `operational_order_id` | `guid`   | Sí        | Identificador de la orden operativa que contiene la asignación. |
| `assignment_id`        | `guid`   | Sí        | Identificador de la asignación operativa. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `x-api-key`     | `{api_key}`      | Sí        |

---

## Request Body

No requiere body. `company_id`, `module_code`, `operational_order_id` y `assignment_id` se toman de la ruta, y `user_id` del token (los campos del command están marcados con `JsonIgnore`).

---

## Flujo del Handler (`SendToUnloadingHandler`)

1. `ValidateAccessAsync(request.UserId, request.CompanyId, request.ModuleCode, ct)`, método heredado de `BaseValidatorHandler<TRequest, TResponse>`: valida que el usuario exista y esté activo, que tenga perfil en la compañía y acceso al módulo (`400` si falla).
2. Carga la asignación operativa (`Id == assignment_id`, `OperationalOrderId == operational_order_id`, `IsActive`, `DeletedAt == null`). Si no existe: `ERP:ASSIGNMENT_NOT_FOUND` (`404`).
3. Valida que la asignación tenga colaboradores: si `HasCollaboratorsAssigned = false` → `ERP:ASSIGNMENT_HAS_NO_COLLABORATORS`.
4. Valida que la asignación tenga maquinaria: si `HasMachineryAssigned = false` → `ERP:ASSIGNMENT_HAS_NO_MACHINERY`.
5. Valida que la asignación tenga observaciones/instrucciones: si `Observations` es nulo o vacío → `ERP:ASSIGNMENT_HAS_NO_OBSERVATIONS`.
6. Setea `assignment.Status = AssignmentOperationalStatus.Pending`, con `UpdateAsync` y `SaveChangesAsync`.
7. Devuelve la respuesta vacía (el controller responde `200 OK` sin body).

> **Contexto de la request:** `company_id`, `module_code`, `operational_order_id` y `assignment_id` los asigna el action desde la ruta, y `user_id` se lee de `HttpContext.Items["UserId"]`.

> **Validación (FluentValidation):** el `SendToUnloadingValidator` valida que `operational_order_id` (`El Id de la orden operativa es requerido`) y `assignment_id` (`El Id de la asignación es requerido`) vengan informados.

> **Sin guardia de estado previo:** a diferencia de `assign-positions` (que exige `Pending`), este endpoint no valida el estado actual de la asignación; desde cualquier estado la fuerza a `Pending`.

---

## Respuestas

### ✅ 200 OK

Respuesta vacía (sin body). La asignación quedó en estado `Pending`.

```json
{}
```

### Notas

| Campo / regla | Descripción |
|---|---|
| Estado resultante | La asignación pasa a `AssignmentOperationalStatus.Pending` (pendiente de descargar). |
| Requisitos previos | Debe tener colaboradores, maquinaria y observaciones/instrucciones. |
| Estado previo | No se valida; el endpoint puede invocarse desde cualquier estado y siempre fuerza `Pending`. |
| Re-ejecución | No está bloqueada; volver a llamarlo mantiene el estado en `Pending`.

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:ASSIGNMENT_HAS_NO_COLLABORATORS",
    "description": "No es posible continuar: la asignación no tiene colaboradores asignados."
  },
  "createdAt": "2026-10-07 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_HAS_NO_COLLABORATORS` | `No es posible continuar: la asignación no tiene colaboradores asignados.` |
| `ERP:ASSIGNMENT_HAS_NO_MACHINERY` | `No es posible continuar: la asignación no tiene maquinaria asignada.` |
| `ERP:ASSIGNMENT_HAS_NO_OBSERVATIONS` | `No es posible continuar: la asignación no tiene instrucciones en el campo "Observaciones".` |
| `Validation_Error` | Errores del `SendToUnloadingValidator`: `El Id de la orden operativa es requerido`, `El Id de la asignación es requerido` |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 404 Not Found

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_NOT_FOUND` | `No se encontró la asignación solicitada.` |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `Forbidden` | `x-api-key` ausente o incorrecta. Lo devuelve el `ApiKeyMiddleware` antes de llegar al controller. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** (fuera de `CoreException`) el `ExceptionMiddleware` responde en **PascalCase**:

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "Server_Error",
    "Description": "Error interno no controlado."
  },
  "CreatedAt": "2026-10-07 14:32:10"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Asignación enviada a descargar exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | `x-api-key` inválida. |
| `404` | La asignación operativa no existe. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `AssignmentOperationalStatus`

Estado de la asignación operativa. Este endpoint fuerza la transición a `Pending`.

| Valor | Nombre | Descripción |
|:---:|---|---|
| `0` | `None` | Sin estado. |
| `1` | `Pending` | Pendiente de descargar. **Estado resultante** tras enviar a bodega. |
| `2` | `InProgress` | Proceso de descarga en curso. |
| `3` | `OnHold` | Pausa de descarga. |
| `4` | `Downloaded` | Descargado. |