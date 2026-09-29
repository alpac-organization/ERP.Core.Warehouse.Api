# Almacén

## Actualizar Sección

Endpoint para actualizar de forma parcial las dimensiones de una sección dentro de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}` |
| **Descripción** | Actualiza `width` y/o `length` de la sección. Si cambian, recalcula la capacidad de la sección (`UpdateSectionAsync`) y actualiza `warehouse_capacity`. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén. Debe existir y estar activo. |
| `section_id`   | `guid`   | Sí        | Identificador de la sección. Debe existir, estar activa y no eliminada. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code`, `warehouse_id` y `section_id` no forman parte del JSON. Debe enviarse **al menos uno** de los campos.

| Parámetro | Tipo      | Requerido | Descripción |
|-----------|-----------|-----------|-------------|
| `width`   | `decimal` | No        | Nuevo ancho (metros). Mayor a cero. Máximo 2 decimales. |
| `length`  | `decimal` | No        | Nuevo largo (metros). Mayor a cero. Máximo 2 decimales. |

```json
{
  "width": 20.00,
  "length": 25.00
}
```

---

## Respuestas

### ✅ 204 No Content

La sección se actualizó correctamente. El cuerpo de la respuesta va vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| PATCH parcial | Debe enviarse al menos un campo (`Debe enviar al menos un campo para actualizar.`). |
| Recálculo | Ocurre si viene `width` o `length`. Usa el valor enviado o el ya guardado en `section_capacity`. |
| Capacidad de sección | Debe existir `section_capacity`. Recibe 400: `La sección no tiene capacidad registrada.` |
| Capacidad de almacén | Debe existir `warehouse_capacity`. Recibe 400: `El almacén no tiene capacidad registrada` |
| Rol | Solo `Administrator`. |
| Sección inválida | `La sección indicada no existe, está inactiva o no pertenece al almacén.` |
| Almacén inválido | `El almacén indicado no existe o no está activo.` |
| Cálculo fallido | `No se pudo calcular la capacidad de la sección.` |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
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
| `204` | Sección actualizada exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
