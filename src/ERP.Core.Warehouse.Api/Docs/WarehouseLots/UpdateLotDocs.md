## Actualizar Tramo

Endpoint para actualizar parcialmente un tramo dentro de una sección de un almacén, en una compañía y módulo. Todos los campos son opcionales: solo se actualizan los enviados en el JSON.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}` |
| **Descripción** | Actualiza los campos enviados del tramo, recalcula su capacidad (`lots_capacity`) y actualiza en cascada las capacidades de la sección (`section_capacity`) y del almacén (`warehouse_capacity`). |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `sections_id` | `guid` | Sí | Identificador único de la sección. |
| `lot_id` | `guid` | Sí | Identificador único del tramo. |

---

## Headers

| Header | Valor | Requerido |
|---|---|---|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id`, `module_code`, `warehouse_id`, `sections_id` y `lot_id` no forman parte del JSON: se toman del token y de la ruta (`[JsonIgnore]`).

Todos los campos son opcionales. Solo se actualizan los campos presentes en el JSON; el resto conserva su valor actual.

| Parámetro | Tipo | Requerido | Descripción |
|---|---|---|---|
| `code` | `string` | No | Código del tramo. Debe ser único dentro de la sección (máx. 50 caracteres). |
| `width_metres` | `decimal` | No | Ancho del tramo (metros). Mayor a 0. |
| `length_metres` | `decimal` | No | Largo del tramo (metros). Mayor a 0. |
| `allows_stacking` | `boolean` | No | Permite apilar en el tramo. |
| `status` | `string (enum)` | No | Estado del tramo (`RackStatus`). Al cambiar el estado se registra `status_changed_at`. |
| `unavailable_reason` | `string` | No | Motivo cuando `status` no es `Available`. |

```json
{
  "width_metres": 12.00,
  "status": "Unavailable",
  "unavailable_reason": "En mantenimiento"
}
```

---

## Respuestas

### ✅ 200 OK

El tramo fue actualizado correctamente y las capacidades fueron recalculadas en cascada. La respuesta es `200` sin cuerpo.

### Notas

| Campo / regla | Descripción |
|---|---|
| Código duplicado | Se valida que no exista otro tramo activo con el mismo código en la sección. |
| Capacidad del tramo | Se recalcula solo si se envían `width_metres` o `length_metres`; en caso contrario conserva las dimensiones almacenadas. |
| Capacidad en cascada | Al actualizar el tramo se recalcula la `section_capacity` incorporando la nueva capacidad, y posteriormente la `warehouse_capacity` del almacén con la sección actualizada. |
| Sección sin capacidad | Si la sección no tiene `section_capacity` registrada, no es posible recalcular y se recibe 400. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El tramo no fue encontrado.",
    "error_codes": ["ERP:LOT_NOT_FOUND"]
  },
  "created_at": "2026-09-10 10:00:00"
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
  "created_at": "2026-09-10 10:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Tramo actualizado exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |