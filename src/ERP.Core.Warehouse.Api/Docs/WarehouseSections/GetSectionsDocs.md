# Almacén

## Listar Secciones

Endpoint para listar (con paginación y filtros) las secciones de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections` |
| **Descripción** | Retorna un listado paginado de secciones del almacén indicado, con filtros opcionales por código, tipo, tipo de almacenaje y estado. Incluye capacidad (área total/disponible/%), dimensiones y coordenadas de layout. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén cuyas secciones se listan. Debe existir, estar activo y no estar eliminado. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Query Params

| Parámetro              | Tipo                        | Requerido | Default | Descripción |
|------------------------|-----------------------------|-----------|---------|-------------|
| `section_code`         | `string`                    | No        | `null`  | Filtra por código de sección exacto. |
| `section_type`         | `enum (SectionType)`        | No        | `null`  | Filtra por tipo de sección (`Storage`, `Aisle`). |
| `section_storage_type` | `enum (SectionStorageType)` | No        | `null`  | Filtra por tipo de almacenaje (`Racks`, `Lots`, `Pallets`, `None`). |
| `is_active`            | `boolean`                   | No        | `null`  | Filtra por estado. Si no se envía, solo se listan secciones activas. |
| `page_number`          | `integer`                   | No        | `1`     | Número de página. Debe ser mayor a cero. |
| `page_size`            | `integer`                   | No        | `10`    | Cantidad de registros por página. Debe ser mayor a cero y no puede exceder `10`. |

---

## Respuestas

### ✅ 200 OK

Retorna un `PagedResponse<SectionDto>`.

```json
{
  "data": [
    {
      "section_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000001",
      "section_code": "ST-01",
      "section_type": "Storage",
      "section_storage_type": "Lots",
      "is_active": true,
      "width": 20.00,
      "length": 10.00,
      "total_area": 200.00,
      "available_area": 50.00,
      "percentage_available_area": 25.00,
      "position_x": 12.50,
      "position_y": 0.00,
      "position_z": 8.00,
      "rotation_y": 90.00
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

### Notas

| Campo / regla | Descripción |
|---|---|
| Orden | Por `created_at` descendente. |
| `total_area` | Mapeado desde `section_capacity.total_area_m2` (Capacidad del progress). |
| `available_area` | Mapeado desde `section_capacity.available_area_with_margin_m2` (Disponible del progress). |
| `percentage_available_area` | Mapeado desde `section_capacity.percentage_available_area_with_margin_m2`. El % usado en UI es `100 - percentage_available_area`. |
| `width` / `length` | Dimensiones desde `section_capacity`. |
| `position_x` / `position_y` / `position_z` / `rotation_y` | Desde `section_coordinates`. Son `null` si la sección no tiene coordenadas registradas. |
| Eliminadas | Excluye secciones con `deleted_at` informado. |
| `is_active` omitido | Filtra `is_active = true`. |
| Almacén inválido | `El almacén indicado no existe o no está activo.` |
| Paginación | 400 si `page_number` o `page_size` no son válidos, o si `page_size` excede 10. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
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
| `200` | Listado paginado de secciones obtenido exitosamente. |
| `400` | Error de validación o de acceso (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
