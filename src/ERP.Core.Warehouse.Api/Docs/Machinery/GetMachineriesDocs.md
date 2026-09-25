# Catálogo de Maquinarias

## Listar Maquinarias

Endpoint para consultar la lista de maquinarias activas de la sucursal del perfil del usuario autenticado.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/machinery` |
| **Descripción** | Obtiene las maquinarias **activas** de la sucursal del usuario autenticado (rama del token, no de la ruta). |
| **Tags**        | `Catálogo de Maquinarias` |

---

## Parámetros de Ruta (Path Params)

| Parámetro     | Tipo     | Requerido | Descripción |
|:------------:|:--------:|-----------|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo dentro de la compañía. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |

---

## Flujo del Handler (`GetMachineriesHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida que el usuario exista y esté activo, y que tenga acceso a la compañía/módulo. Si falla, responde `400`.
2. `branch = access.Profile.BranchId`.
3. Consulta las maquinarias con `BranchId == branch` y `IsActive == true` (`AsNoTracking`) y las mapea a `MachineryListDto` (`Id`, `Brand`, `Code`, `Status`).

> **Validación (FluentValidation):** el `GetMachineriesValidator` (`BaseRequestValidator`) valida `company_id`, `module_code` y `user_id`.

---

## Respuestas

### ✅ 200 OK

Devuelve un arreglo de maquinarias. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`).

```json
[
  {
    "id": "3f2a1b4c-0000-0000-0000-000000000001",
    "brand": "Toyota",
    "code": "MAQ-0001",
    "status": "Available"
  },
  {
    "id": "3f2a1b4c-0000-0000-0000-000000000002",
    "brand": "Cat",
    "code": "MAQ-0002",
    "status": "InUse"
  }
]
```

| Campo    | Tipo                        | Descripción |
|----------|-----------------------------|-------------|
| `id`     | `guid`                      | Identificador de la maquinaria. |
| `brand`  | `string`                    | Marca de la maquinaria. |
| `code`   | `string`                    | Código de la maquinaria. |
| `status` | `enum (MachineryStatus)`    | `Available`, `InUse`, `InMaintenance`, `OutOfService`. |

### ❌ 400 Bad Request

Se lanza como `CoreException` y el `ExceptionMiddleware` la serializa con `JsonNamingPolicy.CamelCase`:

```json
{
  "status": 400,
  "error": {
    "typeError": "Validation_Error",
    "description": "El id de usuario es requerido."
  },
  "createdAt": "2026-09-24 12:00:00"
}
```

| `typeError`      | `description` |
|------------------|---------------|
| `Validation_Error` | Errores del `GetMachineriesValidator`. Con múltiples fallos se unen: `"El id de usuario es requerido. \| El codigo de modulo es requerido."` |
| `ERP:003` | Usuario no encontrado: `"Este usuario no existe!"`. Otros errores de acceso/módulo de `ValidateAccessAsync`. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** (fuera de `CoreException`) el `ExceptionMiddleware` responde en **PascalCase**:

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "Server_Error",
    "Description": "Error interno no controlado."
  },
  "CreatedAt": "2026-09-24 12:00:00"
}
```

> Si el error proviene de un `ThrowInternalError` (CoreException) la respuesta usa camelCase (`status`, `error.typeError`, `error.description`, `createdAt`).

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Lista obtenida exitosamente. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `500` | Error interno del servidor (`Server_Error` PascalCase, o `CoreException` camelCase). |