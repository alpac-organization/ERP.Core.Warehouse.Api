# Asignaciones Operativas

## Asignar Maquinaria

Endpoint para asignar maquinaria a una asignación operativa de una orden operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery` |
| **Descripción** | Asigna una o más maquinarias a la asignación operativa. Valida que exista, esté activa y no esté dada de baja, y activa la bandera `has_machinery_assigned`. |
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

| Parámetro  | Tipo       | Requerido | Descripción |
|------------|------------|:---------:|-------------|
| `machinery` | `guid[]`  | Sí        | Ids de la maquinaria a asignar. Mínimo uno. Los ids repetidos se colapsan con `Distinct()`. |
| `concept`   | `string`   | No        | Concepto de la asignación. Se aplica a todas las maquinarias del lote. |

```json
{
  "machinery": [
    "3f2a1b4c-0000-0000-0000-000000000001",
    "3f2a1b4c-0000-0000-0000-000000000002"
  ],
  "concept": "Carga inicial de bodega"
}
```

---

## Flujo del Handler (`CreateAssignmentMachineryHandler`)

1. `ValidateAssignmentAccessAsync(request, request.AssignmentOperationalId, ct, trackChanges: true)`, método heredado de `BaseAssignmentOperationalHandler<TRequest, TResponse>`: valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo (`400` si falla), carga la asignación operativa con `Include(OperationalOrder)` y valida que exista (`ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND`), que pertenezca a la orden operativa de la ruta (`ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH`) y que la orden sea de la compañía (`ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH`, `403`).
2. Valida la maquinaria: cada id debe existir, tener `is_active = true` y `deleted_at IS NULL`. Los que no cumplen se reportan en `ERP:MACHINERY_NOT_FOUND`.
3. Mapea a entidades `AssignmentsMachinery` e inserta cada una con `created_by_user_id` tomado del token.
4. Activa `has_machinery_assigned` en la asignación operativa y guarda los cambios.

> **`trackChanges: true`:** es obligatorio en los POST y DELETE porque la asignación operativa se modifica y se persiste con `UpdateAsync`. Los GET usan `trackChanges: false` (`AsNoTracking`) porque solo leen.

> **Contexto de la request:** `company_id`, `module_code` y `operational_order_id` los inyecta `BaseAssignmentResourceController.AssignAsync` desde la ruta, y `user_id` lo lee el action de `HttpContext.Items["UserId"]` con `Guid.Parse` antes de delegar. `assignment_id` es el único que asigna el action, porque la command lo llama `AssignmentOperationalId`.

> **Validación (FluentValidation):** el `CreateAssignmentMachineryValidator` (`BaseRequestValidator`) valida `operational_order_id`, `assignment_id`, que `machinery` no esté vacío y que ningún id sea `Guid.Empty`.

> **Alcance de la validación:** es **global**. No se filtra por compañía ni por sucursal, cualquier maquinaria activa del catálogo se puede asignar.

---

## Respuestas

### ✅ 200 OK

La maquinaria se asignó correctamente. El cuerpo de la respuesta va vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Concepto | Se escribe en cada fila insertada de `assignments_machinery`. |
| `has_machinery_assigned` | Se pone en `true` al completar la asignación. |
| `created_by_user_id` | Proviene del claim `sub` del token, no del body. |
| Ids repetidos | Se colapsan con `Distinct()` antes de insertar. |
| Anti-duplicados | **No existe.** Se puede asignar la misma maquinaria varias veces a la misma asignación operativa y el listado mostrará filas repetidas con distinto `assignment_machinery_id`. A diferencia de colaboradores, este endpoint no valida duplicados. |
| Índice en base | No hay índice único que cubra `(assignment_operational_id, machinery_id)`. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:MACHINERY_NOT_FOUND",
    "description": "La maquinaria indicada no existe o no está activa: 9c2b4e17-8d3a-4f60-b1e5-7a9c2d4f6b18"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:MACHINERY_NOT_FOUND` | Alguna maquinaria no existe, está inactiva o está dada de baja. La descripción lista los ids rechazados separados por coma. |
| `ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND` | `La asignacion operativa seleccionada no existe` |
| `ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH` | `La asignacion operativa no pertenece a la orden operativa indicada` |
| `Validation_Error` | Errores del `CreateAssignmentMachineryValidator`: `Debe enviar al menos una maquinaria a asignar`, `El id de la maquinaria es requerido`. |
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
  "CreatedAt": "2026-09-30 14:32:10"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Maquinaria asignada exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | Asignación operativa de otra compañía, o `x-api-key` inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |
