# Asignaciones Operativas

## Iniciar Tarea

Endpoint para iniciar la tarea de una asignación operativa que ya fue enviada a descargar. Requiere que la asignación esté en estado `Pending`; al completarse pasa a `InProgress`. A partir de ese estado el `PATCH .../assignment-positions` ya puede consumirse (asignar posiciones y/o registrar información de polines).

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/start-task` |
| **Descripción** | Inicia la tarea de descarga de la asignación (estado `Pending → InProgress`). |
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

## Flujo del Handler (`StartTaskHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code, ct)`: valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo. Si falla devuelve `400`.
2. Carga la asignación operativa (`Id == assignment_id AND OperationalOrderId == operational_order_id AND IsActive AND DeletedAt == null`). Si no existe: `ERP:ASSIGNMENT_NOT_FOUND`.
3. Si `assignment.Status == InProgress`: `ERP:ASSIGNMENT_IN_PROGRESS` ("La operación ya está en proceso/ejecución.").
4. Si `assignment.Status != Pending`: `ERP:ASSIGNMENT_NOT_PENDING` ("La asignación no está pendiente para iniciar la tarea.").
5. Setea `assignment.Status = InProgress`, actualiza y guarda (`SaveChangesAsync`). Responde `204 No Content`.

---

## Respuestas

### ✅ 204 No Content

Se inició la tarea correctamente. Sin cuerpo de respuesta.

### ❌ 400 Bad Request

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_IN_PROGRESS` | `La operación ya está en proceso/ejecución.` |
| `ERP:ASSIGNMENT_NOT_PENDING` | `La asignación no está pendiente para iniciar la tarea.` |
| `Validation_Error` | Errores del `StartTaskValidator`. |
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
| Estado requerido | La asignación debe estar `Pending` (previamente enviada con `POST .../send-to-unloading`). |
| Efecto | `Pending → InProgress`. |
| Post-requisito | Con `InProgress`, ya puede consumirse el `PATCH .../assignment-positions`. |
| Re-ejecución | Bloqueada: si ya está `InProgress` responde `ERP:ASSIGNMENT_IN_PROGRESS`. |

## Catálogos de Enums Utilizados

### `AssignmentOperationalStatus`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `0` | `None` | Sin estado. |
| `1` | `Pending` | Enviada a descargar; **requerido** para iniciar la tarea. |
| `2` | `InProgress` | En proceso/ejecución; **estado resultante**. |
| `3` | `OnHold` | En pausa; no permite iniciar la tarea. |
| `4` | `Downloaded` | Descargada; no permite iniciar la tarea. |