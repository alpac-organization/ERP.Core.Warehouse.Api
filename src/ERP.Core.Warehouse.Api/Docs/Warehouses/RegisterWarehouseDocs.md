# Almacenes

## Registrar Almacén

Endpoint para registrar un almacén (warehouse) dentro de una compañía/módulo, incluyendo ubicación y capacidad inicial.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse` |
| **Descripción** | Crea el almacén, calcula la capacidad con el calculator del core, persiste `warehouse_capacity` y registra la ubicación asociada (`warehouse_location`). Roles `Operator` y `Supervisor` no tienen permiso. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id` y `module_code` no forman parte del JSON: se toman del token y de la ruta.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `code` | `string` | Sí | Código del almacén. Máximo 20 caracteres. Debe ser único. |
| `warehouse_type` | `enum (WarehouseType)` | Sí | Tipo de almacén (ej. `Fiscal`, `General`). |
| `width` | `decimal` | Sí | Ancho (m). > 0, máx. 2 decimales. |
| `length` | `decimal` | Sí | Largo (m). > 0, máx. 2 decimales. |
| `minimum_height` | `decimal` | Sí | Altura mínima. ≥ 0. |
| `maximum_height` | `decimal` | Sí | Altura máxima. > 0. |
| `has_margins` | `bool` | Sí | Indica si aplica márgenes. |
| `margin_top` / `margin_bottom` / `margin_left` / `margin_right` | `decimal` | Sí | Márgenes. ≥ 0, máx. 2 decimales. |
| `warehouse_location` | `object` | Sí | Datos de ubicación. |
| `warehouse_location.location_name` | `string` | Sí | Nombre de la ubicación. |

```json
{
  "code": "WH-001",
  "warehouse_type": "Fiscal",
  "width": 50.00,
  "length": 40.00,
  "minimum_height": 0.00,
  "maximum_height": 8.00,
  "has_margins": true,
  "margin_top": 1.00,
  "margin_bottom": 1.00,
  "margin_left": 1.00,
  "margin_right": 1.00,
  "warehouse_location": {
    "location_name": "Almacenadora del Pacífico - Managua"
  }
}
```

---

## Respuestas

### 201 Created

Almacén registrado correctamente. Cuerpo típico: `true`.

### 400 Bad Request

Validaciones de entrada, código duplicado, sin permiso o fallo al calcular capacidad (`ErrorResponse`).

### 500 Internal Server Error

Error no controlado del servidor.
