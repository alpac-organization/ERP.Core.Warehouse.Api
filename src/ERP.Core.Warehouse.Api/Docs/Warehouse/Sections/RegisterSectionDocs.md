# Almacén

## Registrar Sección

Endpoint para registrar una sección dentro de un almacén de una compañía/módulo.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections` |
| **Descripción** | Registra una nueva sección en el almacén indicado, calcula su capacidad y actualiza la capacidad disponible del almacén. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén donde se registra la sección. Debe existir y estar activo. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code` y `warehouse_id` no forman parte del JSON: se toman del token y de la ruta (`[JsonIgnore]`).

| Parámetro      | Tipo                 | Requerido | Descripción |
|----------------|----------------------|-----------|-------------|
| `code`         | `string`             | Sí        | Código de la sección. Máximo 50 caracteres. Debe ser único dentro del almacén. |
| `section_type` | `enum (SectionType)` | Sí        | Tipo de sección. Se envía como string. Debe ser un valor válido del enum. |
| `width`        | `decimal`            | Sí        | Ancho de la sección. Debe ser mayor a cero. Admite máximo 2 decimales. |
| `length`       | `decimal`            | Sí        | Largo de la sección. Debe ser mayor a cero. Admite máximo 2 decimales. |

```json
{
  "code": "SEC-A1",
  "section_type": "Storage",
  "width": 12.50,
  "length": 20.00
}
```

---

## Respuestas

### ✅ 201 Created

El recurso se creó correctamente. El cuerpo de la respuesta puede ir vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| `SectionType` | Enum de `ERP.Core.Database.Domain.Enums`. La API lo serializa como string y valida con `IsInEnum`. El catálogo completo de valores no está definido en este repositorio. |
| Rol `Supervisor` | Recibe 400: `No tienes permiso para realizar esta acción`. |
| Capacidad | Recibe 400 si no se puede calcular la capacidad de la sección o si el almacén no tiene capacidad registrada. |
| Código duplicado | Recibe 400: `Ya existe una sección con ese código en el almacén.` |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-09 10:00:00"
}
```

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud"
  },
  "created_at": "2026-09-09 10:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `201` | Sección registrada exitosamente. |
| `400` | Error de validación, acceso del usuario a la compañía/módulo, permisos o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
