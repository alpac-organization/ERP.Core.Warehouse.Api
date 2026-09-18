# Almacén

## Eliminar Sección

Endpoint para eliminar (soft delete) una sección dentro de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `DELETE` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}` |
| **Descripción** | Marca la sección como inactiva (`is_active = false`) y asigna `deleted_at`. No borra la fila ni sus relaciones (capacidad y coordenadas). |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén al que pertenece la sección. |
| `section_id`   | `guid`   | Sí        | Identificador de la sección a eliminar. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Respuestas

### ✅ 204 No Content

La sección se eliminó correctamente. El cuerpo de la respuesta va vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Soft delete | No se elimina el registro. Se persiste `is_active = false` y `deleted_at` con la fecha UTC actual. |
| Relaciones | No se marcan `section_capacity` ni `section_coordinates`. Siguen asociadas a la sección. |
| Búsqueda | El handler busca por `section_id` y `warehouse_id`. |
| No encontrada | Recibe 400: `No se encontró la sección a eliminar`. |
| Almacén distinto | Recibe 400: `La sección no pertenece al almacén indicado.` |

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
| `204` | Sección eliminada (soft delete) exitosamente. |
| `400` | Error de acceso o la sección no existe / no pertenece al almacén (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
