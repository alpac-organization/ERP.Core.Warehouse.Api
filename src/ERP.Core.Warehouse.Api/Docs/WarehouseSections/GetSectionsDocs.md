## Listar Secciones

Endpoint para listar (con paginación y filtros) las secciones de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouse/{warehouse_id}/sections` |
| **Descripción** | Retorna un listado paginado de secciones del almacén indicado, con filtros opcionales por código, tipo, tipo de almacenaje y estado. |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén cuyas secciones se listan. |

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
| `section_type`         | `enum (SectionType)`        | No        | `null`  | Filtra por tipo de sección. Debe ser un valor válido del enum. |
| `section_storage_type` | `enum (SectionStorageType)` | No        | `null`  | Filtra por tipo de almacenaje. |
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
      "section_code": "SEC-A2",
      "section_type": "Aisle",
      "section_storage_type": "Racks",
      "is_active": true
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
| Orden | Los registros se ordenan por `created_at` descendente. |
| `total` | Total de registros que cumplen los filtros, antes de paginar. |
| `is_active` omitido | El handler filtra `is_active = true`. |
| Enums | En query se puede enviar número o nombre. En la respuesta se serializan como **string** (converter global). FluentValidation valida `section_type` con `IsInEnum` (`El tipo de seccion no es válido.`). |
| Valores conocidos en este repo | `SectionType.Aisle`. `SectionStorageType.Lots` y `SectionStorageType.Racks`. El catálogo puede incluir más valores. |
| Paginación | Recibe 400 si `page_number` o `page_size` no son mayores a cero, o si `page_size` excede 10. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-11 12:00:00"
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
  "created_at": "2026-09-11 12:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Listado paginado de secciones obtenido exitosamente. |
| `400` | Error de validación o de acceso del usuario a la compañía/módulo (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
