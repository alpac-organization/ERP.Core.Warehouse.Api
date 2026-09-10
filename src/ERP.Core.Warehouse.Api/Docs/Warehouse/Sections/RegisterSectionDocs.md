## Registrar Sección

Endpoint para registrar una sección dentro de un almacén de una compañía/módulo.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections` |
| **Descripción** | Registra una sección activa en el almacén, calcula y persiste su capacidad (`section_capacity`) y actualiza la capacidad existente del almacén (`warehouse_capacity`). |

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

| Parámetro      | Tipo                         | Requerido | Descripción |
|----------------|------------------------------|-----------|-------------|
| `code`         | `string`                     | Sí        | Código de la sección. Máximo 50 caracteres. Debe ser único dentro del almacén. |
| `section_type` | `integer (enum SectionType)` | Sí        | Tipo de sección. Se envía como **número**. Debe ser un valor definido del enum. Interviene en el cálculo de capacidad. |
| `width`        | `decimal`                    | Sí        | Ancho de la sección (metros). Debe ser mayor a cero. Admite máximo 2 decimales. |
| `length`       | `decimal`                    | Sí        | Largo de la sección (metros). Debe ser mayor a cero. Admite máximo 2 decimales. |

```json
{
  "code": "SEC-A2",
  "section_type": 1,
  "width": 20.00,
  "length": 25.00
}
```

---

## Respuestas

### ✅ 201 Created

El recurso se creó correctamente. El cuerpo de la respuesta puede ir vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Sección | Se crea con `is_active = true`. |
| Capacidad de sección | Se calcula con `width`, `length` y `section_type` y se persiste en `section_capacity` (`width`, `length`, `total_area_m2`, `unused_area_m2`, `available_area_with_margin_m2`, `occupied_chargeable_area_m2`, `unoccupied_chargeable_area_m2`, `percentage_available_area_with_margin_m2`). |
| Capacidad de almacén | El almacén **debe** tener ya un registro en `warehouse_capacity`. Tras el cálculo se actualiza ese registro con el resultado, sin cambiar `capacity_id` ni `warehouse_id`. |
| `SectionType` | Enum de `ERP.Core.Database.Domain.Enums`. En este endpoint se envía como **entero**. FluentValidation valida con `IsInEnum` (`El tipo de sección no es válido.`). El catálogo completo de valores no está definido en este repositorio. |
| `section_type` con tipo incorrecto | Si se envía como string (por ejemplo `"Storage"`), falla la deserialización **antes** del validator (model binding de ASP.NET). |
| Rol `Supervisor` | Recibe 400: `No tienes permiso para realizar esta acción`. |
| Almacén inválido | Recibe 400 desde el handler: `El almacén indicado no existe o no está activo.` |
| Código duplicado | Recibe 400 desde el handler: `Ya existe una sección con ese código en el almacén.` |
| Cálculo fallido | Recibe 400: `No se pudo calcular la capacidad de la sección.` |
| Sin capacidad de almacén | Recibe 400: `El almacén no tiene capacidad registrada`. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`) en errores de validación FluentValidation, acceso y reglas de negocio:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-10 15:00:00"
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
  "created_at": "2026-09-10 15:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `201` | Sección registrada exitosamente. |
| `400` | Error de validación, acceso, permisos, reglas de negocio (`ErrorResponse`) o fallo de deserialización del body (model binding). |
| `500` | Error interno del servidor (`ErrorResponse`). |
