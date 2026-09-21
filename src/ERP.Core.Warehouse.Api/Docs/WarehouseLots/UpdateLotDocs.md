## Actualizar Tramo

Endpoint para actualizar parcialmente un tramo dentro de una sección de un almacén, en una compañía y módulo. Todos los campos son opcionales: solo se actualizan los enviados en el JSON.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}` |
| **Descripción** | Actualiza los campos enviados del tramo (dimensiones físicas y/o matriz de posiciones), recalcula su capacidad (`lots_capacity`) y actualiza en cascada los capacidades de la sección (`section_capacity`) y del almacén (`warehouse_capacity`). |

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
| `nominal_rows` | `integer` | No | Filas nominales del tramo (matriz de posiciones). Mayor a 0. Al enviarse, se **reconstruye la matriz**. |
| `nominal_columns` | `integer` | No | Columnas nominales del tramo. Mayor a 0. Al enviarse, se **reconstruye la matriz**. |
| `width_metres` | `decimal` | No | Ancho del tramo (metros). Mayor a 0. |
| `length_metres` | `decimal` | No | Largo del tramo (metros). Mayor a 0. |
| `allows_stacking` | `boolean` | No | Permite apilar en el tramo. |
| `status` | `string (enum)` | No | Estado del tramo (`RackStatus`). Al cambiar el estado se registra `status_changed_at`. |
| `unavailable_reason` | `string` | No | Motivo cuando `status` no es `Available`. |

> El `code` del tramo **no se actualiza**: se genera mediante el servicio de códigos del core al crear el tramo.

```json
{
  "nominal_rows": 3,
  "nominal_columns": 6,
  "width_metres": 12.00,
  "length_metres": 20.00
}
```

---

## Reconstrucción de la matriz de posiciones

Cuando se envía `nominal_rows` y/o `nominal_columns`, el tramo adopta una nueva matriz `nominal_rows × nominal_columns` de posiciones:

| Regla | Descripción |
|---|---|
| Filas/columnas objetivo | `nominal_rows`/`nominal_columns` enviados, o el valor actual del tramo si no se envían. |
| Posiciones nuevas | Se crean las celdas `(fila, columna)` que no existían, con `PositionCode` `{lotCode}-F{fila}C{columna}` (`Level = 1`). |
| Posiciones sobrantes | Las celdas fuera de la nueva matriz se **eliminan permanentemente** de `lots_positions` (no se conserva historial). |
| Bloqueo por referencias | No se puede reducir la matriz si alguna posición sobrante tiene **cualquier** `stock_placements` (activo o histórico) o `warehouse_assignments` registrado → 400 `ERP:LOT_POSITION_HAS_REFERENCES`. |
| `Lots.NominalRows` / `NominalColumns` | Se actualizan al nuevo tamaño de la matriz. |

---

## Respuestas

### ✅ 200 OK

El tramo fue actualizado correctamente y las capacidades fueron recalculadas en cascada. La respuesta es `200` sin cuerpo.

### Notas

| Campo / regla | Descripción |
|---|---|
| Capacidad del tramo | Se recalcula solo si se envían `width_metres` o `length_metres`; en caso contrario conserva las dimensiones almacenadas. |
| Capacidad en cascada | Al actualizar el tramo se recalcula la `section_capacity` incorporando la nueva capacidad, y posteriormente la `warehouse_capacity` del almacén con la sección actualizada. |
| Sección sin capacidad | Si la sección no tiene `section_capacity` registrada, **no se bloquea la petición**: se actualiza la capacidad del tramo y se omiten los recálculos de sección/almacén. Con `section_capacity`, `width`, `length` y `TotalAreaM2` no son determinables y se entregan en `0`. |
| Matriz sin filas/columnas | Si el tramo no tiene `nominal_rows`/`nominal_columns` y solo se envía uno de los dos, recibe 400 `ERP:LOT_MATRIX_REQUIRED`. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

**Reglas de negocio:**

| Código | Descripción |
|---|---|
| `ERP:LOT_NOT_FOUND` | El tramo no fue encontrado o no pertenece a la sección indicada. |
| `ERP:SECTION_NOT_FOUND` | La sección no existe o no está activa. |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | La sección no pertenece al almacén indicado. |
| `ERP:LOT_MATRIX_REQUIRED` | No se pueden reconstruir las posiciones sin filas y columnas definidas. |
| `ERP:LOT_POSITION_HAS_REFERENCES` | No se pueden eliminar posiciones que tengan stock o asignaciones registradas. |

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