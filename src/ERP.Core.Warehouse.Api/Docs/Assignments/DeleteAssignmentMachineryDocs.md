# Asignaciones Operativas

## Quitar Maquinaria Asignada

Endpoint para quitar (soft delete) una maquinaria de una asignación operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `DELETE` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery/{assignment_machinery_id}` |
| **Descripción** | Soft-delete de la asignación de maquinaria y recálculo de la bandera `has_machinery_assigned`. |
| **Tags**        | `Asignaciones operacionales` |

---

## Parámetros de Ruta (Path Params)

| Parámetro                 | Tipo     | Requerido | Descripción |
|:-------------------------:|:--------:|:---------:|-------------|
| `company_id`              | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`             | `string` | Sí        | Código del módulo dentro de la compañía. |
| `operational_order_id`    | `guid`   | Sí        | Identificador de la orden operativa que contiene la asignación. |
| `assignment_id`           | `guid`   | Sí        | Identificador de la asignación operativa. |
| `assignment_machinery_id` | `guid`   | Sí        | Identificador de la asignación de maquinaria. Es el `assignment_machinery_id` que devuelve el listado, **no** el `machinery_id`. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `x-api-key`     | `{api_key}`      | Sí        |

> El endpoint **no lleva body**. Los tres ids viajan por la ruta.

---

## Flujo del Handler (`DeleteAssignmentMachineryHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla, responde `400`.
2. Busca la asignación operativa con `Include(OperationalOrder)`. Si no existe: `ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND`.
3. Verifica que `OperationalOrderId` coincida con el de la ruta: `ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH`.
4. Verifica que la orden operativa pertenezca a la compañía: `ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH` (`403`).
5. Busca la asignación de maquinaria filtrando por `Id`, `AssignmentOperationalId`, `IsActive = true` y `DeletedAt IS NULL`. Si no existe: `ERP:ASSIGNMENT_MACHINERY_NOT_FOUND`.
6. Consulta `hasRemainingMachinery`: si queda alguna otra maquinaria activa en la misma asignación operativa.
7. Marca `is_active = false` y `deleted_at = now` en la fila.
8. Actualiza `has_machinery_assigned` con el resultado del paso 6 y guarda los cambios.

> **Validación (FluentValidation):** el `DeleteAssignmentMachineryValidator` (`BaseRequestValidator`) valida `operational_order_id`, `assignment_id` y `assignment_machinery_id` no vacíos ni `Guid.Empty`.

---

## Respuestas

### ✅ 204 No Content

La maquinaria se quitó correctamente. El cuerpo de la respuesta va vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Soft delete | `is_active = false` y `deleted_at` con hora UTC. La fila **no** se elimina de la base. |
| `has_machinery_assigned` | Queda en `false` solo si era la última maquinaria activa de la asignación. En `true` si queda alguna. |
| Borrado repetido | Un `DELETE` sobre el mismo id responde `400`, no `204`: la fila ya no cumple `is_active` ni `deleted_at IS NULL`. |
| Id de otra asignación | Si el `assignment_machinery_id` pertenece a otra asignación operativa, responde `400` (`ERP:ASSIGNMENT_MACHINERY_NOT_FOUND`). |
| Maquinaria repetida | Si la maquinaria estaba asignada dos veces, cada fila tiene su propio `assignment_machinery_id` y hay que borrarlas una por una. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:ASSIGNMENT_MACHINERY_NOT_FOUND",
    "description": "La maquinaria asignada no existe"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_MACHINERY_NOT_FOUND` | `La maquinaria asignada no existe`. También cuando ya fue quitada o pertenece a otra asignación operativa. |
| `ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND` | `La asignacion operativa seleccionada no existe` |
| `ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH` | `La asignacion operativa no pertenece a la orden operativa indicada` |
| `Validation_Error` | Errores del `DeleteAssignmentMachineryValidator`: `La orden operativa es requerida`, `El id de la asignacion operativa es requerido`, `El id de la maquinaria asignada es requerido`. |
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
| `204` | Maquinaria quitada (soft delete) exitosamente. |
| `400` | Error de validación, acceso o si la asignación no existe (`ErrorResponse` camelCase). |
| `403` | Asignación operativa de otra compañía, o `x-api-key` inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |
