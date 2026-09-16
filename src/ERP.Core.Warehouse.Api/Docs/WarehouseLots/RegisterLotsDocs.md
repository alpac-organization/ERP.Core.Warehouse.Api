## Registrar Lotes (Masivo)

Endpoint para registrar de 1 a 10 tramos de forma masiva dentro de una sección de tipo tramos (`SectionStorageType.Lots`) de un almacén, en una compañía y módulo. Todos los tramos se crean con las mismas filas, columnas, ancho y largo; solo difieren en el código generado. Cada tramo genera sus posiciones (matriz filas × columnas) y su capacidad (`lots_capacity`), y actualiza en cascada las capacidades de la sección (`section_capacity`) y del almacén (`warehouse_capacity`) para todo el lote.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots` |
| **Descripción** | Registra hasta 10 tramos activos en la sección, crea sus posiciones y recalcula las capacidades en cascada. |

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

El `code` de cada tramo **no se envía**: lo genera el `CodeGenerator` del core con el formato `{SectionCode}-LOT-{NN}` (ej: `SEC-A-LOT-01`), secuencial y único dentro de la sección.

Los campos llevan el mismo nombre que las entidades para evitar mapeos: `nominal_rows` y `nominal_columns` (`Lots`), `width` y `length` (`LotsCapacity`).

| Campo | Tipo | Requerido | Descripción |
|---|---|---|---|
| `quantity` | `integer` | Sí | Cantidad de tramos a crear. Mínimo 1, máximo 10. |
| `nominal_rows` | `integer` | Sí | Filas nominales de cada tramo (matriz de posiciones). Mayor a 0. |
| `nominal_columns` | `integer` | Sí | Columnas nominales de cada tramo. Mayor a 0. |
| `width` | `decimal` | Sí | Ancho de cada tramo (metros). Mayor a 0, máximo 2 decimales. |
| `length` | `decimal` | Sí | Largo de cada tramo (metros). Mayor a 0, máximo 2 decimales. |

```json
{
  "quantity": 2,
  "nominal_rows": 4,
  "nominal_columns": 5,
  "width": 10.00,
  "length": 15.00
}
```

---

## Defaults del tramo y sus posiciones

No se envían en la petición; se aplican automáticamente:

| Campo | Valor |
|---|---|
| `Lots.Status` | `Available`. |
| `Lots.AllowsStacking` | `true`. |
| `Lots.UnavailableReason` / `StatusChangedAt` | `null`. |

---

## Posiciones generadas

Por cada tramo se crea una **matriz `nominal_rows × nominal_columns`** de posiciones:

| Regla | Valor |
|---|---|
| `PositionCode` | `{lotCode}-{fila}{columna}` (concatenación de fila y columna, tipo índice de matriz). Ej: tramo `SEC-A-LOT-01`, fila 3 columna 2 → `SEC-A-LOT-01-32`. |
| `Row`, `Column` | Índices de la matriz (filas y columnas 1..N). |
| `Level` | Siempre `1` (a piso). Los niveles de estibado se registran por otro flujo. |
| `Status` | `Available`. |
| `AllowsStocking` | `true`. |

> ⚠️ Al concatenar fila y columna sin separador, un tramo de más de 9 filas o columnas puede generar códigos ambiguos (ej: fila 1 columna 23 y fila 12 columna 3 producen el mismo sufijo `123`).

---

## Respuestas

### ✅ 201 Created

Los tramos fueron creados correctamente. La respuesta es `201` sin cuerpo.

### Notas

| Campo / regla | Descripción |
|---|---|
| Límite por petición | Mínimo 1 y máximo 10 tramos por petición (campo `quantity`). |
| Límite por sección | Cada sección admite un máximo de 10 tramos activos en total. Si ya hay `N` tramos existentes, se permite crear hasta `10 − N`; si se excede, 400 `ERP:SECTION_LOT_LIMIT_EXCEEDED`. |
| Tipo de sección | La sección debe ser tipo `SectionStorageType.Lots`. Las secciones tipo pasillo (`SectionType.Aisle`) o que no admiten tramos reciben 400. |
| Código generado | El core genera `{SectionCode}-LOT-{NN}` secuencial (máximo existente + 1) por cada tramo. Si la sección no tiene `code`, recibe 400 `ERP:SECTION_CODE_NOT_FOUND`. |
| Capacidad del tramo | Se calcula con `width` y `length` y se persiste en `lots_capacity` (área M2). |
| Capacidad en cascada | El batch se calcula de una sola vez (`CalculateLotsAsync`) incorporando todas las capacidades nuevas y actualizando `section_capacity` y `warehouse_capacity`. |
| Sección sin capacidad | Si la sección no tiene `section_capacity` registrada, no es posible recalcular y se recibe 400. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "Se permite un máximo de 10 tramos por petición.",
    "error_codes": []
  },
  "created_at": "2026-09-16 10:00:00"
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
  "created_at": "2026-09-16 10:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `201` | Tramos registrados exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |