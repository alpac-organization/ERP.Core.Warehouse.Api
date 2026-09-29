# Almacén

## Eliminar Sección

Endpoint para eliminar (soft delete) una sección dentro de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `DELETE` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}` |
| **Descripción** | Soft-delete de la sección y de su capacidad. Recalcula la capacidad del almacén excluyendo la sección eliminada. No permite eliminar si tiene tramos o racks activos. |

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
| Soft delete | `is_active = false` y `deleted_at` con hora de Nicaragua (`NicaraguaClock.Now`). |
| Capacidad de sección | También se marca `section_capacity.deleted_at`. |
| Recálculo de almacén | `DeleteSectionAsync` excluye la sección del total y actualiza `warehouse_capacity`. |
| Hijos activos | Si tiene lots o racks no eliminados: `No se puede eliminar la sección porque aún tiene tramos o racks activos...` (`ERP:SECTION_HAS_CHILDREN`) |
| Rol | Solo `Administrator`. |
| No encontrada | `No se encontró la sección a eliminar` (`ERP:NOT_FOUND`) |
| Fallo de recálculo | `No se pudo recalcular la capacidad del almacén al eliminar la sección.` |
| Sin capacidad de almacén | `El almacén no tiene capacidad registrada` |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "No se puede eliminar la sección porque aún tiene tramos o racks activos. Muévelos a otra sección o elimínalos antes de continuar."
  },
  "created_at": "2026-09-28 12:00:00"
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
  "created_at": "2026-09-28 12:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `204` | Sección eliminada (soft delete) exitosamente. |
| `400` | Error de acceso, permisos, hijos activos o recálculo (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
