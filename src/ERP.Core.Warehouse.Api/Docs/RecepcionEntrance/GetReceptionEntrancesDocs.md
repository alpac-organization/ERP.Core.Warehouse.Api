# Control de Acceso

## Listar Registros de Recepción

Endpoint para listar (con paginación y filtros) los registros de entrada de recepción.

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` |
| **Descripción** | Retorna un listado paginado de registros de recepción, con filtros opcionales por día, placa, número de documento, número de contenedor y tipo de documento. |

---

## Parámetros de Ruta (Path Params)

| Parámetro     | Tipo     | Requerido | Descripción |
|:-------------:|:--------:|-----------|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo dentro de la compañía. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Query Params

| Parámetro           | Tipo                        | Requerido | Default | Descripción |
|---------------------|-----------------------------|-----------|---------|-------------|
| `only_day`          | `boolean`                   | No        | `true`  | Si `true`, filtra solo los registros del día actual. Si `false`, incluye todos los días. |
| `plate_number`      | `string`                    | No        | `null`  | Filtra por número de placa del vehículo (búsqueda parcial). |
| `document_number`   | `string`                    | No        | `null`  | Filtra por número de documento (DUCA o declaración aduanera). |
| `container_number`  | `string`                    | No        | `null`  | Filtra por número de contenedor. |
| `document_type`     | `integer (enum DocumentType)` | No      | `null`  | Filtra por tipo de documento: `1 = DUCA`, `2 = CustomsDeclaration`. Se envía como **número**. |
| `page_number`       | `integer`                   | No        | `1`     | Número de página. Debe ser mayor a cero. |
| `page_size`         | `integer`                   | No        | `10`    | Cantidad de registros por página. Debe ser mayor a cero. |

---

## Respuestas

### ✅ 200 OK

Retorna un `PagedResponse<ReceptionEntranceDto>`.

```json
{
  "data": [
    {
      "reception_code": "REC-20260930-001",
      "reception_entrance_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000001",
      "seal_number": "SEAL-12345",
      "container_number": "CONT-987654",
      "country_of_origin": "China"
    },
    {
      "reception_code": "REC-20260930-002",
      "reception_entrance_id": "5f8d0d55-6c8a-4a2b-9d3f-000000000002",
      "seal_number": "SEAL-54321",
      "container_number": "CONT-111222",
      "country_of_origin": "USA"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 2
}
```

**Estructura de `ReceptionEntranceDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `reception_code` | `string \| null` | Código generado automáticamente para la recepción (ej: `REC-20260930-001`). |
| `reception_entrance_id` | `guid` | Identificador único del registro de recepción. |
| `seal_number` | `string` | Número de precinto/sello. |
| `container_number` | `string` | Número de contenedor. |
| `country_of_origin` | `string` | País de origen. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Orden | Por `created_at` descendente (más recientes primero). |
| `only_day = true` | Filtra por fecha de creación igual a la fecha actual (UTC). |
| Filtros de texto | `plate_number`, `document_number`, `container_number` usan búsqueda parcial (contiene). |
| `document_type` | Filtra exacto por el enum `DocumentType` (1 = DUCA, 2 = CustomsDeclaration). |
| Paginación | 400 si `page_number` o `page_size` no son válidos. |

### ❌ 400 Bad Request

Error de validación, acceso o reglas de negocio usando `ErrorResponse`:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-30 12:00:00"
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
  "created_at": "2026-09-30 12:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Listado paginado de registros de recepción obtenido exitosamente. |
| `400` | Error de validación o de acceso (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |