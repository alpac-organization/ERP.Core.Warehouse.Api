## Detalle de Sección

Endpoint para obtener el detalle de una sección dentro de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections/{section_id}/details` |
| **Descripción** | Retorna el detalle de una sección (`SectionDetailsDto`): código, estado, capacidad y coordenadas. |

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
  "section_code": "SEC-A2",
  "is_active": true,
  "capacity": {
    "section_capacity_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000011",
    "width": 20.00,
    "length": 25.00,
    "unused_area_m2": 10.50,
    "available_area_with_margin_m2": 420.00,
    "total_area_m2": 500.00,
    "unoccupied_chargeable_area_m2": 380.00,
    "occupied_chargeable_area_m2": 40.00,
    "percentage_available_area_with_margin_m2": 84.00
  },
  "coordinates": {
    "position_x": 0,
    "position_y": 0,
    "position_z": 0
  }
}
```

### Notas

| Campo / regla | Descripción |
|---|---|
| Búsqueda | El handler localiza la sección por `section_id` e incluye `section_capacity`. |
| `capacity` | Se mapea desde `SectionCapacity`. Si la sección no tiene capacidad, el objeto puede ir vacío o nulo. |
| `coordinates` | El mapper ignora este miembro, así que `position_x`, `position_y` y `position_z` salen con el valor por defecto (`0`). |
| No encontrada | Recibe 400: `No se encontro el detalle de esta solicitud`. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-10 15:00:00"
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
  "created_at": "2026-09-10 15:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Detalle de la sección obtenido exitosamente. |
| `400` | Error de acceso o la sección no existe (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
