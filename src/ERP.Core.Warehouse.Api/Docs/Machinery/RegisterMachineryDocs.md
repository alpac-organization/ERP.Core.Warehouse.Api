# Catálogo de Maquinarias

## Registrar Maquinaria

Endpoint para registrar una nueva maquinaria en el catálogo, en la sucursal del perfil del usuario autenticado.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/machinery` |
| **Descripción** | Registra una maquinaria en la sucursal del usuario autenticado (rama del token) y la persiste con `Status = Available`. |
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

## Request Body

El JSON usa **`SnakeCaseLower`** (`PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower`) y es **case-insensitive** (body deserializado por `AddJsonOptions`).

> **Nota:** `branch_id` **no se utiliza**: la sucursal se toma del perfil del usuario autenticado (`access.Profile.BranchId`), y el handler ignora el valor enviado.

| Parámetro       | Tipo     | Requerido | Descripción |
|-----------------|----------|-----------|-------------|
| `branch_id`     | `guid`   | No        | Ignorado; la sucursal se toma del perfil del token. |
| `brand`         | `string` | Sí        | Marca de la maquinaria (máx. 100). |
| `code`          | `string` | Sí        | Código de la maquinaria (máx. 50). |
| `year`          | `int`    | Sí        | Año de la maquinaria (1900 – año vigente). |
| `model`         | `string` | Sí        | Modelo (máx. 100). |
| `serial_number` | `string` | Sí        | Número de serie (máx. 100). |

### Ejemplo

```json
{
  "brand": "Toyota",
  "code": "MAQ-0001",
  "year": 2023,
  "model": "7FDU35",
  "serial_number": "7FDU-2023-000123"
}
```

---

## Flujo del Handler (`RegisterMachineryHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida que el usuario exista y esté activo, y que tenga acceso a la compañía/módulo. Si falla, responde `400`.
2. Si el rol es **`Administrator`** → `400` con `typeError = "ERP:INVALID_ACCESS"`: `"No tienes acceso para realizar esta acción"`.
3. `branch = access.Profile.BranchId`.
4. Crea la entidad `Machinery` con `Status = Available` e `IsActive = true` (helper `MachineryProfile.ToMachineryEntity`).
5. `RegisterMachinery` + `SaveChangesAsync` → devuelve `true`.

> **Validación (FluentValidation):** el `RegisterMachineryValidator` (`BaseRequestValidator`) valida, además de `company_id`, `module_code` y `user_id`, las reglas de negocio del body (`brand`, `code`, `model`, `serial_number` requeridos/con límite y `year` en rango). Los mensajes de las reglas falladas se unen con ` | `.

---

## Respuestas

### ✅ 200 OK

```
true
```

### ❌ 400 Bad Request

Se lanza como `CoreException` y el `ExceptionMiddleware` la serializa con `JsonNamingPolicy.CamelCase`:

```json
{
  "status": 400,
  "error": {
    "typeError": "Validation_Error",
    "description": "La marca de la maquinaria es obligatoria."
  },
  "createdAt": "2026-09-24 12:00:00"
}
```

| `typeError`      | `description` |
|------------------|---------------|
| `Validation_Error` | Errores del `RegisterMachineryValidator`. Con múltiples fallos se unen: `"La marca de la maquinaria es obligatoria. \| El modelo de la maquinaria es obligatoria."` |
| `ERP:INVALID_ACCESS` | Rol `Administrator`: `"No tienes acceso para realizar esta acción"`. |
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
| `200` | Maquinaria registrada exitosamente. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `500` | Error interno del servidor (`Server_Error` PascalCase, o `CoreException` camelCase). |