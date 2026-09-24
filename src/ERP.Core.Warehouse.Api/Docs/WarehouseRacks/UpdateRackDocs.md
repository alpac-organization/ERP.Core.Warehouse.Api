## Actualizar Rack (Actualización Integral / Parcial)

Endpoint para modificar de forma parcial o total las propiedades operativas, dimensiones métricas o coordenadas espaciales de un rack existente dentro de una sección de un almacén.

---

## Información General

| Campo | Valor |
|---|---|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks/{rack_id}` |
| **Descripción** | Actualiza los atributos enviados del rack, ajusta sus coordenadas en el plano y, si cambian las dimensiones, recalcula en cascada las capacidades de la sección y del almacén. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `section_id` | `guid` | Sí | Identificador único de la sección. |
| `rack_id` | `guid` | Sí | Identificador único del rack a actualizar. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

> **Nota:** Todos los campos del cuerpo son opcionales. Solo se modifican aquellos atributos presentes en el JSON enviado; los demás preservan su valor actual en base de datos.

| Campo | Tipo | Requerido | Descripción / Reglas |
|---|---|:---:|---|
| `row_number` | `integer` | No | Número de hilera o pasillo físico. Mayor a `0`. |
| `usage_profile` | `string` / `int` | No | Perfil operativo (`ActiveFlow`, `StaticHold`). |
| `status` | `string` / `int` | No | Estado operativo (`Available`, `Occupied`, `UnderMaintenance`, `Blocked`). Al modificarse se actualiza automáticamente `status_changed_at` con la fecha y hora UTC actual. |
| `unavailable_reason` | `string` | No | Motivo cuando el rack pasa a mantenimiento o bloqueo. |
| `width` | `decimal` | No | Nuevo ancho del rack en metros. Mayor a `0`. |
| `length` | `decimal` | No | Nuevo largo del rack en metros. Mayor a `0`. |
| `height` | `decimal` | No | Nueva altura del rack en metros. Mayor a `0`. |
| `position_x` | `decimal` | No | Coordenada X en metros para el lienzo 2D / 3D. |
| `position_y` | `decimal` | No | Coordenada Y en metros para el lienzo 2D / 3D. |
| `position_z` | `decimal` | No | Coordenada Z (elevación del suelo) en metros. |
| `rotation_y` | `decimal` | No | Ángulo de rotación en el eje Y en grados (ej. `90.00` para orientación vertical). |

### Ejemplo 1: Mover o rotar rack en el plano 2D
```json
{
  "position_x": 5.08,
  "position_y": 6.50,
  "rotation_y": 90.00
}
```

### Ejemplo 2: Poner rack en mantenimiento
```json
{
  "status": "UnderMaintenance",
  "unavailable_reason": "Reparación estructural de largueros nivel 2"
}
```

### Ejemplo 3: Redimensionar y recalcular capacidad volumétrica
```json
{
  "width": 1.10,
  "length": 2.50,
  "height": 4.80
}
```

---

## Respuestas

### ✅ 200 OK

El rack fue actualizado correctamente. La respuesta es un código HTTP `200 OK` sin cuerpo.

---

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El rack no fue encontrado.",
    "error_codes": ["ERP:RACK_NOT_FOUND"]
  },
  "created_at": "2026-09-23 17:38:00"
}
```

#### Catálogo de Errores:
| Código de Error | Descripción |
|---|---|
| `ERP:SECTION_NOT_FOUND` | La sección especificada no existe o no está activa. |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | La sección no pertenece al almacén indicado. |
| `ERP:RACK_NOT_FOUND` | El rack no existe, fue eliminado o no pertenece a la sección. |
| `ERP:SECTION_CAPACITY_NOT_FOUND` | La sección no cuenta con registro de capacidad para recalcular en cascada. |

---

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud."
  },
  "created_at": "2026-09-23 17:38:00"
}
```
