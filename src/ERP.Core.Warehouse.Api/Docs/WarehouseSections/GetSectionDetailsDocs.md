# Almacén

## Detalle de Sección

Endpoint para obtener el detalle de una sección dentro de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/details` |
| **Descripción** | Retorna el detalle de una sección (`SectionDetailsDto`): código, tipo, estado, máximo de polines (pasillo), capacidad y coordenadas. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén. |
| `section_id`   | `guid`   | Sí        | Identificador de la sección a consultar. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Respuestas

### ✅ 200 OK

Retorna un `SectionDetailsDto`.

```json
{
  "section_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000001",
  "section_code": "SP-01",
  "is_active": true,
  "max_pallets_per_level_aisle": 2.00,
  "section_type": "Aisle",
  "section_storage_type": "Pallets",
  "capacity": {
    "section_capacity_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000011",
    "width": 3.50,
    "length": 40.00,
    "unused_area_m2": 10.50,
    "available_area_with_margin_m2": 120.00,
    "total_area_m2": 140.00,
    "unoccupied_chargeable_area_m2": 100.00,
    "occupied_chargeable_area_m2": 20.00,
    "percentage_available_area_with_margin_m2": 85.71
  },
  "coordinates": {
    "section_coordinate_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000021",
    "position_x": 1.00,
    "position_y": 2.00,
    "position_z": 0.00,
    "rotation_y": 90.00
  }
}
```

### Notas

| Campo / regla | Descripción |
|---|---|
| Filtro | Solo secciones activas y no eliminadas (`deleted_at == null` e `is_active`). |
| `max_pallets_per_level_aisle` | Máximo de polines por nivel del pasillo. En secciones sin valor puede aparecer como `0`. |
| `capacity` | Mapeado desde `SectionCapacity`. |
| `coordinates` | Mapeado desde `SectionCoordinates`. Si no hay coordenadas, puede ir vacío o con valores por defecto. |
| No encontrada | Recibe 400: `No se encontro el detalle de esta sección`. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "No se encontro el detalle de esta sección"
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
| `200` | Detalle de la sección obtenido exitosamente. |
| `400` | Error de acceso o la sección no existe / está inactiva (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
