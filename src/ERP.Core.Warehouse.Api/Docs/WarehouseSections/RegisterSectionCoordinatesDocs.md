# Almacén

## Registrar Coordenadas de Sección

Endpoint para registrar las coordenadas 3D de una sección dentro de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/coordinates` |
| **Descripción** | Persiste las coordenadas de la sección (`position_x`, `position_y`, `position_z`, `rotation_y`) en `section_coordinates`. Solo permite el alta inicial; si ya existen coordenadas, responde error. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén al que pertenece la sección. |
| `section_id`   | `guid`   | Sí        | Identificador de la sección. Debe existir, estar activa y no estar eliminada. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code`, `warehouse_id` y `section_id` no deben enviarse en el JSON: el controlador los toma del token y de la ruta.

| Parámetro    | Tipo      | Requerido | Descripción |
|--------------|-----------|-----------|-------------|
| `position_x` | `decimal` | Sí        | Coordenada X. Máximo 2 decimales. |
| `position_y` | `decimal` | Sí        | Coordenada Y. Máximo 2 decimales. |
| `position_z` | `decimal` | Sí        | Coordenada Z. Máximo 2 decimales. |
| `rotation_y` | `decimal` | Sí        | Rotación en Y. Debe estar entre `0` y `360`. Máximo 2 decimales. |

```json
{
  "position_x": 1.00,
  "position_y": 2.00,
  "position_z": 0.00,
  "rotation_y": 90.00
}
```

---

## Respuestas

### ✅ 201 Created

Las coordenadas se registraron correctamente. El cuerpo de la respuesta puede ir vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Persistencia | Se crea un registro en `section_coordinates` ligado a `section_id`. |
| Relación 1:1 | Una sección solo puede tener un registro de coordenadas. |
| Ya existen | Recibe 400: `La sección indicada ya tiene coordenadas.` |
| Sección inválida | Recibe 400: `La sección indicada no existe o no está activa.` |
| Almacén distinto | Recibe 400: `La sección no pertenece al almacén indicado.` |
| Rol | Solo `Administrator`. |
| Actualización posterior | Para mover o rotar, usar `PATCH .../layout`. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "La sección indicada ya tiene coordenadas."
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
| `201` | Coordenadas de sección registradas exitosamente. |
| `400` | Error de validación, acceso o la sección no existe / ya tiene coordenadas (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
