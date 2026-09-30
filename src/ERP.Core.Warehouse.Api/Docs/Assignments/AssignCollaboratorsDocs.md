# Asignaciones Operativas

## Asignar Colaboradores

Endpoint para asignar colaboradores a una asignación operativa de una orden operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/collaborators` |
| **Descripción** | Asigna uno o más colaboradores a la asignación operativa. Valida que existan, que pertenezcan a la sucursal del usuario autenticado y que no hayan sido asignados antes a la misma asignación, y activa la bandera `has_collaborators_assigned`. |
| **Tags**        | `Asignaciones operacionales` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo dentro de la compañía. |
| `operational_order_id` | `guid`   | Sí        | Identificador de la orden operativa que contiene la asignación. |
| `assignment_id`        | `guid`   | Sí        | Identificador de la asignación operativa. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `x-api-key`     | `{api_key}`        | Sí        |
| `Content-Type`  | `application/json` | Sí        |

---

## Request Body

`company_id`, `module_code`, `operational_order_id`, `assignment_id` y `user_id` no forman parte del JSON: se toman de la ruta y del token.

| Parámetro      | Tipo                                | Requerido | Default               | Descripción |
|----------------|-----------------------------|:---------:|---------|-------------|
| `collaborators`| `guid[]`                    | Sí        | —       | Ids de los colaboradores a asignar. Mínimo uno. Los ids repetidos se colapsan con `Distinct()`. |
| `role`         | `enum (AssignmentCollaboratorsRoles)` | No | `WarehouseAssistant` | Rol **de la asignación**, no el cargo real del colaborador. Se aplica a todo el lote. |

Valores de `role`:

| Valor               | Descripción        |
|---------------------|--------------------|
| `WarehouseAssistant` | Auxiliar de bodega (default). |
| `ForkliftOperator`   | Operador de montacargas. |

```json
{
  "collaborators": [
    "8a1f3c7e-0000-0000-0000-000000000001",
    "8a1f3c7e-0000-0000-0000-000000000002"
  ],
  "role": "WarehouseAssistant"
}
```

> También se acepta el valor numérico (`1` o `2`), pero se recomienda el texto.

---

## Flujo del Handler (`CreateAssignmentCollaboratorsHandler`)

1. `ValidateAssignmentAccessAsync(request, request.AssignmentOperationalId, ct, trackChanges: true)`, método heredado de `BaseAssignmentOperationalHandler<TRequest, TResponse>`: valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo (`400` si falla), carga la asignación operativa con `Include(OperationalOrder)` y valida que exista (`ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND`), que pertenezca a la orden operativa de la ruta (`ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH`) y que la orden sea de la compañía (`ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH`, `403`). Devuelve también `branchId` (`access.Profile.BranchId`), que es el valor que usa el paso 2.
2. **Valida sucursal:** consulta `collaborators` con `Include(WorkingInformation)` y filtra `WorkingInformation.BranchId == branchId`. Los que no coinciden se reportan en `ERP:COLLABORATOR_NOT_FOUND`.
3. **Valida duplicados:** busca filas en `assignment_collaborators` con el mismo `AssignmentOperationalId` y cualquiera de los `CollaboratorId` recibidos. Los que ya existen se reportan en `ERP:COLLABORATOR_ALREADY_ASSIGNED`.
4. Mapea a entidades `AssignmentCollaborators` e inserta cada una con `created_by_user_id` tomado del token.
5. Activa `has_collaborators_assigned` en la asignación operativa y guarda los cambios.

> **`trackChanges: true`:** es obligatorio en los POST y DELETE porque la asignación operativa se modifica y se persiste con `UpdateAsync`. Los GET usan `trackChanges: false` (`AsNoTracking`) porque solo leen.

> **Contexto de la request:** `company_id`, `module_code` y `operational_order_id` los inyecta `BaseAssignmentResourceController.AssignAsync` desde la ruta, y `user_id` lo lee el action de `HttpContext.Items["UserId"]` con `Guid.Parse` antes de delegar. `assignment_id` es el único que asigna el action, porque la command lo llama `AssignmentOperationalId`.

> **Validación (FluentValidation):** el `CreateAssignmentCollaboratorsValidator` (`BaseRequestValidator`) valida `operational_order_id`, `assignment_id`, que `collaborators` no esté vacío, que ningún id sea `Guid.Empty` y que `role` sea un valor del enum.

> **Alcance de la validación de sucursal:** la referencia es la **sucursal del perfil del usuario autenticado** (`branchId`), no la de la bodega de la asignación. No se valida cargo (`JobPositionId`), área (`AreaId`), ni estado (`IsActive`, `HasBeenFired`, `Status`) del colaborador. La validación es solo de sucursal.

---

## Respuestas

### ✅ 200 OK

Los colaboradores se asignaron correctamente. El cuerpo de la respuesta va vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Rol de la asignación | `role` se escribe igual en todas las filas del lote. No se puede asignar un rol distinto a cada colaborador en el mismo POST. |
| `has_collaborators_assigned` | Se pone en `true` al completar la asignación. |
| `created_by_user_id` | Proviene del claim `sub` del token, no del body. |
| Ids repetidos | Se colapsan con `Distinct()` antes de insertar. |
| Colaborador sin ficha | Un colaborador sin `WorkingInformation` cargada queda excluido: no hay forma de demostrar a qué sucursal pertenece. |
| Índice en base | No hay índice único que cubra `(assignment_operational_id, collaborator_id)`. La prevención de duplicados es solo de aplicación: dos requests concurrentes con el mismo colaborador pueden pasar los dos e insertar dos filas. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:COLLABORATOR_ALREADY_ASSIGNED",
    "description": "El colaborador ya fue asignado a esta asignacion operativa: 8a1f3c7e-0000-0000-0000-000000000001"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:COLLABORATOR_NOT_FOUND` | Algún colaborador no existe o no pertenece a la sucursal del usuario autenticado. La descripción lista los ids rechazados separados por coma. |
| `ERP:COLLABORATOR_ALREADY_ASSIGNED` | Algún colaborador ya fue asignado a esta asignación operativa. La descripción lista los ids repetidos. |
| `ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND` | `La asignacion operativa seleccionada no existe` |
| `ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH` | `La asignacion operativa no pertenece a la orden operativa indicada` |
| `Validation_Error` | Errores del `CreateAssignmentCollaboratorsValidator`: `Debe enviar al menos un colaborador a asignar`, `El id del colaborador es requerido`, `El rol de los colaboradores no es válido. Valores permitidos: WarehouseAssistant (auxiliar de bodega) o ForkliftOperator (operador de montacargas)`. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH` | `No tienes acceso a la asignacion operativa seleccionada` |
| `Forbidden` | `x-api-key` ausente o incorrecta. |

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
|---|---|
| `200` | Colaboradores asignados exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | Asignación operativa de otra compañía, o `x-api-key` inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |
