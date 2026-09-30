# Almacenes

## Actualizar Almacén (PATCH)

Endpoint para actualizar parcialmente un almacén: código, estado, tipo, ubicación y/o capacidad (recalculada con el calculator del core).

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}` |
| **Descripción** | Actualización parcial. Solo se modifican los campos enviados. Si se envía `capacity`, se recalcula con `UpdateWarehouseAsync` y se persiste. Roles `Operator` y `Supervisor` no tienen permiso. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo. |
| `warehouse_id` | `guid` | Sí | Identificador del almacén a actualizar. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id`, `module_code` y `warehouse_id` se toman del token/ruta (`[JsonIgnore]`).

Debe enviarse **al menos un** campo a actualizar.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `code` | `string` | No | Nuevo código. Máx. 20. Único entre almacenes no eliminados. |
| `is_active` | `bool` | No | Activa/desactiva el almacén. |
| `warehouse_type` | `enum (WarehouseType)` | No | Nuevo tipo. |
| `location` | `object` | No | Actualización de ubicación. |
| `location.location_name` | `string` | Condicional | Nombre de ubicación (si se envía, no vacío). |
| `capacity` | `object` | No | Inputs de capacidad (no métricas calculadas). |
| `capacity.width` / `length` | `decimal?` | No | > 0 si se envían. |
| `capacity.has_margins` | `bool?` | No | Flag de márgenes. |
| `capacity.minimum_height` / `maximum_height` | `decimal?` | No | Alturas; máx ≥ mín si ambas vienen. |
| `capacity.margin_*` | `decimal?` | No | ≥ 0 si se envían. |

### Ejemplo: solo código y estado

```json
{
  "code": "WH-001-UPD",
  "is_active": true
}
```

### Ejemplo: ubicación y capacidad

```json
{
  "location": {
    "location_name": "Almacenadora Pacífico - Nave B"
  },
  "capacity": {
    "width": 55.00,
    "length": 40.00,
    "has_margins": true,
    "minimum_height": 0.00,
    "maximum_height": 8.00,
    "margin_top": 1.00,
    "margin_bottom": 1.00,
    "margin_left": 1.00,
    "margin_right": 1.00
  }
}
```

---

## Respuestas

### 200 OK

Actualización correcta. Cuerpo vacío (`Ok`).

### 400 Bad Request

Validaciones, sin campos a actualizar, código duplicado, sin permiso, ubicación/capacidad inexistente, o fallo al recalcular.

Códigos típicos:

| Código | Descripción |
|--------|-------------|
| `ERP:01` | Permiso / código duplicado / cálculo fallido. |
| `ERP:WAREHOUSE_LOCATION_NOT_FOUND` | Sin ubicación registrada. |
| `ERP:WAREHOUSE_CAPACITY_NOT_FOUND` | Sin capacidad registrada. |

### 404 Not Found

| Código | Descripción |
|--------|-------------|
| `ERP:WAREHOUSE_NOT_FOUND` | El almacén no existe. |

### 500 Internal Server Error

Error no controlado del servidor.
