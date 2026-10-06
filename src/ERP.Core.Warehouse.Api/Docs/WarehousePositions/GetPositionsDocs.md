# Posiciones

## Obtener Posiciones por Sección (Tramos y Racks)

Endpoint para obtener todas las posiciones de una sección, agrupadas por bloque según el tipo de almacenamiento de la sección: si la sección es de **tramos** (`SectionStorageType.Lots`) los bloques son tramos y sus posiciones; si es de **racks** (`SectionStorageType.Racks`) los bloques son racks y sus posiciones. Cada posición incluye su estado, nivel y coordenadas cartesianas (`PositionX/Y/Z`, `RotationY`) cuando están registradas.

> Las secciones de tipo pasillo/pallets (`SectionStorageType.Pallets` / `None`) aún no soportan posiciones por tramo o rack y responden `400`.

## Información General

| Campo | Valor |
|---|---|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/positions` |
| **Descripción** | Retorna las posiciones de la sección agrupadas por bloque (tramos o racks), con sus coordenadas cuando existen. |
| **Tags** | `Posiciones` |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `section_id` | `guid` | Sí | Identificador único de la sección. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |
| `X-Api-Key` | `{api_key}` | Sí |

---

## Parámetros de Query

| Parámetro | Tipo | Requerido | Default | Descripción |
|---|---|:---:|:---:|---|
| `tramo_id` | `guid` | No | `null` | Filtra los bloques al tramo indicado (solo aplica en secciones de tramos). |
| `rack_id` | `guid` | No | `null` | Filtra los bloques al rack indicado (solo aplica en secciones de racks). |
| `status` | `enum (RackStatus)` | No | `null` | Filtra las posiciones por estado exacto. |

> La lista no está paginada: retorna todos los bloques y posiciones de la sección que cumplan los filtros.

---

## Flujo del Handler (`GetPositionsHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Consulta `Sections` con `AsNoTracking`, `Id == section_id AND WarehouseId == warehouse_id AND IsActive AND DeletedAt == null`, cargando en un solo query:
   - `Lots` (tramos no eliminados) → `Positions` (no eliminadas, filtradas por `status`) → `LotsPositionsCoordinates`.
   - `Racks` (racks no eliminados) → `Positions` (no eliminadas, filtradas por `status`) → `RacksPositionsCoordinates`.
   - El filtro `tramo_id` / `rack_id` se aplica dentro del `Include` del bloque correspondiente.
3. Si la sección no existe o no está activa: `ERP:SECTION_NOT_FOUND`.
4. Si `section.SectionStorageType` no es `Lots` ni `Racks`: `ERP:POSITIONS_NOT_SUPPORTED`.
5. Mapea con `WarehousePositionsProfile` a `GetPositionsDto`:
   - Sección de tramos → bloques = tramos ordenados por `Code` ascendente.
   - Sección de racks → bloques = racks ordenados por `RowNumber` y luego `Code`.
   - Posiciones ordenadas por `Row`, `Column`, `Level`.
   - `Coordinates` = `null` si la posición no tiene coordenadas registradas o estas fueron eliminadas (soft delete).
6. Devuelve `GetPositionsDto` sin paginación.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y `warehouse_id`, `section_id`, `tramo_id`, `rack_id` y `status` los asigna el action.

> **Validación (FluentValidation):** el `GetPositionsValidator` valida `warehouse_id` y `section_id` requeridos y distintos de `Guid.Empty`, que `tramo_id` y `rack_id` no sean `Guid.Empty` cuando vienen informados, y que `status` sea un valor válido del enum.

---

## Respuestas

### ✅ 200 OK

Devuelve un `GetPositionsDto`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`). Las fechas van en ISO 8601.

```json
{
  "warehouse_id": "c9b2d12e-0001-4444-8888-abcdef012345",
  "section_id": "f8a964a3-76a0-4fc7-bf98-251f28b4d081",
  "section_code": "RACKS-A",
  "section_type": "Storage",
  "section_storage_type": "Racks",
  "blocks": [
    {
      "id": "e2a48b32-0001-4444-8888-abcdef012345",
      "code": "RACK-01",
      "positions": [
        {
          "id": "3b2e591c-1111-4444-9999-012345abcdef",
          "code": "RACK-01-N1P1",
          "level": 1,
          "status": "Available",
          "coordinates": {
            "position_x": 2.50,
            "position_y": 6.50,
            "position_z": 1.00,
            "rotation_y": 0.00
          }
        },
        {
          "id": "4c3f692d-2222-4444-aaaa-123456789abc",
          "code": "RACK-01-N2P1",
          "level": 2,
          "status": "Occupied",
          "coordinates": null
        }
      ]
    }
  ]
}
```

#### Campos

| Campo | Tipo | Descripción |
|---|---|---|
| `warehouse_id` | `guid` | Identificador del almacén de la sección. |
| `section_id` | `guid` | Identificador de la sección consultada. |
| `section_code` | `string` | Código de la sección (ej. `RACKS-A`, `TRAMOS-B`). |
| `section_type` | `enum (SectionType)` | Tipo de sección: `Storage`, `Aisle`. |
| `section_storage_type` | `enum (SectionStorageType)` | `Racks` (bloques = racks) o `Lots` (bloques = tramos). |
| `blocks[].id` | `guid` | Id del bloque: id del tramo (sección `Lots`) o id del rack (sección `Racks`). Es el valor que consumen `tramo_id`/`rack_id`. |
| `blocks[].code` | `string` | Código del bloque (tramo o rack). |
| `blocks[].positions[].id` | `guid` | Id de la posición. |
| `blocks[].positions[].code` | `string` | Código de la posición. |
| `blocks[].positions[].level` | `integer` | Nivel de la posición en altura. |
| `blocks[].positions[].status` | `enum (RackStatus)` | Estado de la posición. |
| `blocks[].positions[].coordinates` | `object \| null` | Coordenadas de la posición. `null` si no están registradas o fueron eliminadas. |
| `coordinates.position_x` | `decimal` | Coordenada X (metros). |
| `coordinates.position_y` | `decimal` | Coordenada Y (metros). |
| `coordinates.position_z` | `decimal` | Coordenada Z / altura (metros). |
| `coordinates.rotation_y` | `decimal` | Rotación en el eje Y (grados). |

#### Notas

| Campo / regla | Descripción |
|---|---|
| Tramo vs Rack | Si `section_storage_type = Lots`, `blocks` son tramos (entidad `Lots`). Si `Racks`, son racks (entidad `Racks`). Nunca se mezclan. |
| Orden de bloques | Tramos por `code` ascendente. Racks por `row_number` y luego `code`. |
| Orden de posiciones | Por `row`, `column`, luego `level`. |
| Filtros | `tramo_id` filtra solo bloques de tramos; `rack_id` solo bloques de racks. `status` filtra posiciones. |
| Sin paginación | La respuesta incluye todos los bloques y posiciones que cumplan los filtros. |
| Secciones no soportadas | Pasillos/Pallets (`SectionStorageType` distinto de `Lots`/`Racks`) responden `ERP:POSITIONS_NOT_SUPPORTED`. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:SECTION_NOT_FOUND",
    "description": "La sección indicada no existe o no está activa."
  },
  "createdAt": "2026-10-06 14:32:10"
}
```

| `typeError` | `description` |
|---|---|
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:POSITIONS_NOT_SUPPORTED` | `La sección no soporta posiciones por tramo o rack.` |
| `Validation_Error` | Errores del `GetPositionsValidator`: `El id del almacén es requerido.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `El id del tramo no es válido.`, `El id del rack no es válido.`, `El estado de la posición no es válido.` |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 403 Forbidden

| `typeError` | `description` |
|---|---|
| `Forbidden` | `X-Api-Key` ausente o incorrecta. Lo devuelve el `ApiKeyMiddleware` antes de llegar al controller. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** (fuera de `CoreException`) el `ExceptionMiddleware` responde en **PascalCase**:

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "Server_Error",
    "Description": "Error interno no controlado."
  },
  "CreatedAt": "2026-10-06 14:32:10"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Posiciones obtenidas exitosamente. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `RackStatus`

Aplica al estado de cada posición (y también al de tramos y racks).

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Available` | Lista y libre para asignar mercadería. |
| `2` | `Occupied` | Tiene mercadería asignada actualmente. |
| `3` | `UnderMaintenance` | Fuera de servicio por mantenimiento. |
| `4` | `Blocked` | Inhabilitado por otra causa. |
| `5` | `Reserved` | Apartado para una operación en curso. |

### `SectionStorageType`

Define el tipo de bloques que devuelve el endpoint.

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Racks` | Sección de racks: `blocks` son racks con sus `RackPositions`. |
| `2` | `Lots` | Sección de tramos: `blocks` son tramos con sus `LotsPositions`. |
| `3` | `Pallets` | No soportado para este endpoint. |
| `4` | `None` | No soportado para este endpoint. |

### `SectionType`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Storage` | Sección de almacenamiento. |
| `2` | `Aisle` | Pasillo (sin posiciones por tramo/rack por ahora). |