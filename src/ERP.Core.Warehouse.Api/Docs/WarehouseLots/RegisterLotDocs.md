## Registrar Tramo

Endpoint para registrar un tramo dentro de una sección de tipo tramos (`SectionStorageType.Lots`) de un almacén, en una compañía y módulo.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots` |
| **Descripción** | Registra un tramo activo en la sección, calcula su capacidad (`lots_capacity`) y actualiza en cascada las capacidades de la sección (`section_capacity`) y del almacén (`warehouse_capacity`) incluyendo el nuevo tramo. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `sections_id` | `guid` | Sí | Identificador único de la sección. |

---

## Headers

| Header | Valor | Requerido |
|---|---|---|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id`, `module_code`, `warehouse_id` y `sections_id` no forman parte del JSON: se toman del token y de la ruta (`[JsonIgnore]`).

| Parámetro | Tipo | Requerido | Descripción |
|---|---|---|---|
| `code` | `string` | Sí | Código del tramo. Debe ser único dentro de la sección. |
| `width_metres` | `decimal` | Sí | Ancho del tramo (metros). Mayor a 0. |
| `length_metres` | `decimal` | Sí | Largo del tramo (metros). Mayor a 0. |
| `allows_stacking` | `boolean` | No | Permite apilar en el tramo. Por defecto `true`. |
| `status` | `string (enum)` | No | Estado del tramo (`Available` por defecto). |
| `unavailable_reason` | `string` | No | Motivo cuando `status` no es `Available`. |

```json
{
  "code": "LOT-A1",
  "width_metres": 10.00,
  "length_metres": 15.00
}
```

---

## Respuestas

### ✅ 201 Created

El tramo fue creado correctamente. La respuesta es `201` sin cuerpo.

### Notas

| Campo / regla | Descripción |
|---|---|
| Tipo de sección | La sección debe ser tipo `SectionStorageType.Lots`. Las secciones tipo pasillo (`SectionType.Aisle`) o que no admiten tramos reciben 400. |
| Código duplicado | Se valida que no exista un tramo con el mismo código en la sección. |
| Capacidad del tramo | Se calcula con `width_metres` y `length_metres` y se persiste en `lots_capacity` (área M2). |
| Capacidad en cascada | Al crear el tramo se recalcula la `section_capacity` incorporando la nueva capacidad, y posteriormente la `warehouse_capacity` del almacén con la sección actualizada. |
| Sección sin capacidad | Si la sección no tiene `section_capacity` registrada, no es posible recalcular y se recibe 400. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "Ya existe un tramo con el código 'LOT-A1' en la sección.",
    "error_codes": ["ERP:LOT_CODE_ALREADY_EXISTS"]
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
| `201` | Tramo registrado exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |