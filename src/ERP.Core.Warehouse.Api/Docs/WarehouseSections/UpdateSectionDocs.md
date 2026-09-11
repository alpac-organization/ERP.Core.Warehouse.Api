## Actualizar Sección

Endpoint para actualizar de forma parcial una sección dentro de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections/{section_id}` |
| **Descripción** | Actualiza los campos enviados de la sección. Si cambian `width` o `length`, recalcula la capacidad de la sección (`UpdateSectionAsync`) y actualiza `warehouse_capacity`. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén al que pertenece la sección. Debe existir y estar activo. |
| `section_id`   | `guid`   | Sí        | Identificador de la sección a actualizar. Debe existir en ese almacén, estar activa y no estar eliminada. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code`, `warehouse_id` y `section_id` no forman parte del JSON: se toman del token y de la ruta (`[JsonIgnore]`). Todos los campos del body son opcionales, pero debe enviarse **al menos uno**.

| Parámetro              | Tipo                        | Requerido | Descripción |
|------------------------|-----------------------------|-----------|-------------|
| `code`                 | `string`                    | No        | Nuevo código. Se recorta (`Trim`). Si se envía, no puede estar vacío, máximo 50 caracteres y debe ser único en el almacén. |
| `width`                | `decimal`                   | No        | Nuevo ancho (metros). Debe ser mayor a cero. Admite máximo 2 decimales. Si se envía, dispara recálculo de capacidad. |
| `length`               | `decimal`                   | No        | Nuevo largo (metros). Debe ser mayor a cero. Admite máximo 2 decimales. Si se envía, dispara recálculo de capacidad. |
| `is_active`            | `boolean`                   | No        | Activa o desactiva la sección. |
| `section_type`         | `enum (SectionType)`        | No        | Nuevo tipo de sección. Debe ser un valor válido del enum. No dispara recálculo de capacidad. |
| `section_storage_type` | `enum (SectionStorageType)` | No        | Nuevo tipo de almacenaje. Debe ser un valor válido del enum. No dispara recálculo de capacidad. |

```json
{
  "code": "SEC-A3",
  "section_type": "Aisle",
  "section_storage_type": "Racks",
  "is_active": true,
  "width": 20.00,
  "length": 25.00
}
```

---

## Respuestas

### ✅ 200 OK

El recurso se actualizó correctamente. El cuerpo de la respuesta puede ir vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| PATCH parcial | Solo se aplican los campos presentes. Debe enviarse al menos uno (`Debe enviar al menos un campo para actualizar.`). |
| Recálculo | Ocurre **solo** si viene `width` o `length`. Usa el valor enviado o el `width`/`length` ya guardado en `section_capacity`. Llama a `UpdateSectionAsync`. |
| Capacidad de sección | Si hay que recalcular, la sección **debe** tener `section_capacity`. No se crea un registro nuevo. Recibe 400: `La sección no tiene capacidad registrada.` |
| Capacidad de almacén | El almacén debe tener `warehouse_capacity`; se actualiza tras el cálculo. |
| Enums | Converter global `JsonStringEnumConverter`. FluentValidation: `El tipo de sección no es válido.` / `El tipo de almacenaje para secciones no es válido.` |
| Valores conocidos en este repo | `SectionType.Aisle`. `SectionStorageType.Lots` y `SectionStorageType.Racks`. El catálogo puede incluir más valores. |
| Rol `Supervisor` | Recibe 400: `No tienes permiso para realizar esta acción`. |
| Sección inválida | Recibe 400: `La sección indicada no existe o no pertenece al almacén.` (también si no está activa o está eliminada). |
| Almacén inválido | Recibe 400: `El almacén indicado no existe o no está activo.` |
| Código duplicado | Recibe 400: `Ya existe una sección con ese código en el almacén.` |
| Sin medidas | Recibe 400: `No se puede recalcular la capacidad porque la sección no tiene ancho y largo registrados.` |
| Cálculo fallido | Recibe 400: `No se pudo calcular la capacidad de la sección.` |
| Sin capacidad de almacén | Recibe 400: `El almacén no tiene capacidad registrada`. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-11 12:00:00"
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
  "created_at": "2026-09-11 12:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Sección actualizada exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
