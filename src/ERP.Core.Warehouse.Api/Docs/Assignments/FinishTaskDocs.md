# Asignaciones Operativas

## Finalizar Tarea

Endpoint para finalizar la tarea de una asignación operativa en proceso de descarga. Requiere que la asignación esté en estado `InProgress`, que tenga **al menos una posición de almacén asignada** (`assignment_stock_placements`) y que tenga **información de polines registrada** (`HasPositionatingInformation = true`). Al completarse:

- La asignación pasa de `InProgress → Downloaded`.
- Las posiciones reservadas de la asignación pasan de `Reserved → Occupied` (la mercadería queda almacenada).

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/finish-task` |
| **Descripción** | Finaliza la tarea de descarga de la asignación (estado `InProgress → Downloaded`) y ocupa las posiciones reservadas. |
| **Tags**        | `Asignaciones Operativas` |

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

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `x-api-key`     | `{api_key}`        | Sí        |

No requiere body.

---

## Flujo del Handler (`FinishTaskHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code, ct)`: valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo. Si falla devuelve `400`.
2. Carga la asignación operativa (`Id == assignment_id AND OperationalOrderId == operational_order_id AND IsActive AND DeletedAt == null`) incluyendo sus `AssignmentStockPlacements` con las posiciones de tramos y racks. Si no existe: `ERP:ASSIGNMENT_NOT_FOUND`.
3. Si `assignment.Status == Downloaded`: `ERP:ASSIGNMENT_DOWNLOADED` ("La operación ya fue finalizada.").
4. Si `assignment.Status != InProgress`: `ERP:ASSIGNMENT_NOT_IN_PROGRESS` ("La asignación no está en proceso.").
5. Si `!assignment.HasPositionatingInformation`: `ERP:ASSIGNMENT_NO_POSITIONATING_INFORMATION` ("Debe registrar la información de polines antes de finalizar la tarea.").
6. Si no hay `AssignmentStockPlacements`: `ERP:ASSIGNMENT_NO_POSITIONS` ("Debe asignar al menos una posición de almacén antes de finalizar la tarea.").
7. Por cada posición asignada (stock placement) que esté `Reserved`, la marca `Occupied` (`RackStatus`) y actualiza.
8. Setea `assignment.Status = Downloaded`.
9. Verifica si quedan **asignaciones activas** de la misma orden operativa en estado distinto a `Downloaded` (excluyendo la actual, que aún está `InProgress` a nivel BD). Si **no** quedan: setea `OperationalOrder.Status = Completed` y actualiza la OP.
10. Actualiza la asignación y guarda (`SaveChangesAsync`). Responde `204 No Content`.

---

## Respuestas

### ✅ 204 No Content

Tarea finalizada correctamente. Sin cuerpo de respuesta.

### ❌ 400 Bad Request

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_DOWNLOADED` | `La operación ya fue finalizada.` |
| `ERP:ASSIGNMENT_NOT_IN_PROGRESS` | `La asignación no está en proceso.` |
| `ERP:ASSIGNMENT_NO_POSITIONATING_INFORMATION` | `Debe registrar la información de polines antes de finalizar la tarea.` |
| `ERP:ASSIGNMENT_NO_POSITIONS` | `Debe asignar al menos una posición de almacén antes de finalizar la tarea.` |
| `Validation_Error` | Errores del `FinishTaskValidator`. |
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
  "CreatedAt": "2026-10-08 14:32:10"
}
```

---

## Notas

| Regla | Descripción |
|---|---|
| Estado requerido | La asignación debe estar `InProgress` (previamente iniciada con `POST .../start-task`). |
| Precondiciones | Debe tener `HasPositionatingInformation = true` y al menos una posición asignada (`assignment_stock_placements`). |
| Efecto | `InProgress → Downloaded`. Las posiciones asignadas pasan de `Reserved → Occupied`. |
| Orden operativa | Si **todas** las asignaciones activas de la misma OP quedan `Downloaded`, la OP pasa a `Completed`. |
| Re-ejecución | Bloqueada: si ya está `Downloaded` responde `ERP:ASSIGNMENT_DOWNLOADED`. |
| Post-condición | Con `Downloaded`, el `PATCH .../assignment-positions` ya no puede consumirse. |

## Catálogos de Enums Utilizados

### `OperationalOrderStatus`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Completed` | Documentación completada; **estado resultante** cuando todas las asignaciones de la OP están `Downloaded`. |
| `2` | `PendingDocument` | Pendiente de llenar los detalles. |
| `3` | `Assignment` | Enviada a asignación. |

### `AssignmentOperationalStatus`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `0` | `None` | Sin estado. |
| `1` | `Pending` | Enviada a descargar; no permite finalizar la tarea. |
| `2` | `InProgress` | En proceso/ejecución; **requerido** para finalizar la tarea. |
| `3` | `OnHold` | En pausa; no permite finalizar la tarea. |
| `4` | `Downloaded` | Descargada; **estado resultante**. |

### `RackStatus`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Available` | Libre. |
| `2` | `Occupied` | Ocupada; **estado resultante** al finalizar la tarea. |
| `3` | `UnderMaintenance` | Fuera de servicio por mantenimiento. |
| `4` | `Blocked` | Inhabilitada. |
| `5` | `Reserved` | Reservada por una tarea en ejecución; pasa a `Occupied` al finalizar. |