# Almacén

## Actualizar Layout de Sección

Endpoint para actualizar el layout de una sección: coordenadas y/o dimensiones.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/layout` |
| **Descripción** | Actualiza posición (`position_x`, `position_y`, `position_z`, `rotation_y`) y/o dimensiones (`width`, `length`). Si cambian dimensiones, recalcula capacidad de sección y almacén. Requiere que la sección ya tenga coordenadas registradas. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén. Debe existir, estar activo y no eliminado. |
| `section_id`   | `guid`   | Sí        | Identificador de la sección. Debe existir, estar activa y no eliminada. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code`, `warehouse_id` y `section_id` no forman parte del JSON. Debe enviarse **al menos uno** de los campos.

| Parámetro    | Tipo      | Requerido | Descripción |
|--------------|-----------|-----------|-------------|
| `position_x` | `decimal` | No        | Nueva coordenada X. Máximo 2 decimales. |
| `position_y` | `decimal` | No        | Nueva coordenada Y. Máximo 2 decimales. |
| `position_z` | `decimal` | No        | Nueva coordenada Z. Máximo 2 decimales. |
| `rotation_y` | `decimal` | No        | Nueva rotación en Y. Entre `0` y `360`. Máximo 2 decimales. |
| `width`      | `decimal` | No        | Nuevo ancho (metros). Mayor a cero. Máximo 2 decimales. Dispara recálculo de capacidad. |
| `length`     | `decimal` | No        | Nuevo largo (metros). Mayor a cero. Máximo 2 decimales. Dispara recálculo de capacidad. |

Ejemplo solo posición:

```json
{
  "position_x": 10.50,
  "position_y": 20.00,
  "position_z": 0.00,
  "rotation_y": 90.00
}
```

Ejemplo posición y dimensiones:

```json
{
  "position_x": 10.50,
  "position_y": 20.00,
  "width": 22.00,
  "length": 30.00
}
```

---

## Respuestas

### ✅ 204 No Content

El layout se actualizó correctamente. El cuerpo de la respuesta va vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Coordenadas previas | La sección **debe** tener coordenadas (registradas con `POST .../coordinates`). Si no: `La sección no tiene coordenadas registradas. Regístralas antes de actualizar el layout.` (`ERP:SECTION_COORDINATES_NOT_FOUND`) |
| Solo actualización | No crea coordenadas; solo actualiza las existentes. |
| Recálculo | Solo si viene `width` o `length`. |
| Body vacío | Recibe 400: `Debe enviar al menos un campo para actualizar (position_x, position_y, position_z, rotation_y, width o length).` |
| Rol | Solo `Administrator`. |
| Sección inválida | `La sección indicada no existe, está inactiva o no pertenece al almacén.` |
| Almacén inválido | `El almacén indicado no existe o no está activo.` |
| Sin capacidad | `La sección no tiene capacidad registrada.` / `El almacén no tiene capacidad registrada` |
| Cálculo fallido | `No se pudo calcular la capacidad de la sección.` |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "La sección no tiene coordenadas registradas. Regístralas antes de actualizar el layout."
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
| `204` | Layout de sección actualizado exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
