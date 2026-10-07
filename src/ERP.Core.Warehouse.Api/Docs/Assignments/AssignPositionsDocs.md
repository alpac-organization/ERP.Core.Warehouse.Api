# Asignaciones Operativas

## Asignar Posiciones (Tramos y Racks)

Endpoint para asignar posiciones de almacén (de tramos y/o racks) a una asignación operativa de una orden operativa. Las posiciones deben estar `Available`; al asignarlas pasan a `Reserved` y se registra el histórico en `assignment_stock_placements`. Al finalizar, la asignación pasa a estado `InProgress` y se generan los códigos QR y de barras del voucher.

> La asignación debe haber sido enviada a descargar (estado `Pending`). Si ya está `InProgress`, el endpoint responde `ERP:ASSIGNMENT_IN_PROGRESS` y no procesa la petición.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/assignment-positions` |
| **Descripción** | Asigna posiciones de tramos y racks a la asignación operativa, las reserva, registra el histórico y genera los códigos QR/barras del voucher. |
| **Tags**        | `Asignaciones operacionales` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo dentro de la compañía (ej. `WAREHOUSE`). |
| `operational_order_id` | `guid`   | Sí        | Identificador de la orden operativa que contiene la asignación. |
| `assignment_id`        | `guid`   | Sí        | Identificador de la asignación operativa. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `x-api-key`     | `{api_key}`        | Sí        |
| `Content-Type`  | `application/json` | Sí        |

---

## Request Body

`company_id`, `module_code`, `operational_order_id`, `assignment_id` y `user_id` no forman parte del JSON: se toman de la ruta y del token.

El body se compone de una lista de `sections`. Cada sección indica su `section_id` y declara las posiciones a asignar en sus **tramos** (`tramos`) y/o en sus **racks** (`racks`). Cada bloque referencia el `block_id` (id del tramo o del rack) y la lista de `position_ids` a reservar.

| Parámetro                       | Tipo      | Requerido | Descripción |
|---------------------------------|-----------|:---------:|-------------|
| `sections`                      | `array`   | Sí        | Lista de secciones con las posiciones a asignar. Mínimo una. |
| `sections[].section_id`         | `guid`    | Sí        | Id de la sección. |
| `sections[].tramos`             | `array`   | No        | Bloques de tramos de la sección. Solo válido si la sección es de tipo `Lots`. |
| `sections[].tramos[].block_id`  | `guid`    | Sí        | Id del tramo (entidad `Lots`). |
| `sections[].tramos[].position_ids` | `guid[]` | Sí       | Ids de las posiciones del tramo a asignar. Mínimo uno. |
| `sections[].racks`               | `array`   | No        | Bloques de racks de la sección. Solo válido si la sección es de tipo `Racks`. |
| `sections[].racks[].block_id`   | `guid`    | Sí        | Id del rack (entidad `Racks`). |
| `sections[].racks[].position_ids` | `guid[]` | Sí       | Ids de las posiciones a asignar. Mínimo una. |

```json
{
  "sections": [
    {
      "section_id": "f8a964a3-76a0-4fc7-bf98-251f28b4d081",
      "tramos": [
        {
          "block_id": "e2a48b32-0001-4444-8888-abcdef012345",
          "position_ids": [
            "3b2e591c-1111-4444-9999-012345abcdef",
            "4c3f692d-2222-4444-aaaa-123456789abc",
            "5d4a7b3e-3333-4444-bbbb-234567890bcd"
          ]
        }
      ],
      "racks": []
    },
    {
      "section_id": "a1b2c3d4-0002-4444-8888-abcdef012345",
      "tramos": [],
      "racks": [
        {
          "block_id": "b2c3d4e5-0003-4444-8888-abcdef012345",
          "position_ids": [
            "c3d4e5f6-4444-4444-cccc-345678901cde",
            "d4e5f6a7-5555-4444-dddd-456789012def"
          ]
        }
      ]
    },
    {
      "section_id": "e5f6a7b8-0004-4444-8888-abcdef012345",
      "tramos": [
        {
          "block_id": "f6a7b8c9-0005-4444-8888-abcdef012345",
          "position_ids": [
            "a7b8c9d0-6666-4444-eeee-567890123ef0",
            "b8c9d0e1-7777-4444-ffff-678901234f01"
          ]
        },
        {
          "block_id": "c9d0e1f2-0006-4444-8888-abcdef012345",
          "position_ids": [
            "d0e1f2a3-8888-4444-0000-789012345a12",
            "e1f2a3b4-9999-4444-1111-890123456b23"
          ]
        }
      ],
      "racks": []
    }
  ]
}
```

---

## Flujo del Handler (`AssignMerchandiseDesignatedLocationHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code, ct)`: valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo. Si falla devuelve `400`.
2. Carga la asignación operativa (`Id == assignment_id AND OperationalOrderId == operational_order_id AND IsActive AND DeletedAt == null`). Si no existe: `ERP:ASSIGNMENT_NOT_FOUND`.
3. Si `assignment.Status == InProgress`: `ERP:ASSIGNMENT_IN_PROGRESS` ("La operación ya está en proceso/ejecución."). No procesa la petición.
4. Si `assignment.Status != Pending`: `ERP:ASSIGNMENT_NOT_SENT` ("La asignación aún no ha sido enviada a descargar.").
5. Recoge todos los `position_ids` del body; si hay repetidos (global, entre todos los bloques): `ERP:DUPLICATED_POSITIONS`.
6. Consulta `Sections` en un solo query (`AsSingleQuery`) por los `section_id` recibidos, cargando dentro de cada `Include` solo lo no eliminado (`DeletedAt == null`):
   - `Lots` → `Positions` (posiciones de tramos).
   - `Racks` → `Positions` (posiciones de racks).
   Si no se encuentran todas las secciones: `ERP:SECTION_NOT_FOUND`.
7. Por cada sección solicitada valida:
   - Que pertenezca al almacén de la asignación (`section.WarehouseId == assignment.WarehouseId`): si no, `ERP:POSITION_SECTION_MISMATCH`.
   - Que si envía `tramos`, la sección sea de tipo `Lots`; si envía `racks`, sea de tipo `Racks`: si no, `ERP:POSITION_SECTION_MISMATCH`.
   - Por cada tramo: que el `block_id` pertenezca a la sección (`ERP:POSITION_SECTION_MISMATCH`); que cada posición exista en el tramo (`ERP:POSITION_NOT_FOUND`); que cada posición esté `Available` (`ERP:POSITION_NOT_AVAILABLE`).
   - Por cada rack: mismas validaciones que en tramos (mensajes equivalentes).
8. Acumula las posiciones a reservar en dos listas (`(LotsPositions, SectionId)` y `(RackPositions, SectionId)`).
9. Itera ambas listas: setea `Position.Status = RackStatus.Reserved`, actualiza con `UpdateAsync` y registra el histórico con `AssignmentStockPlacements.AssignStockPlacement(...)`, incluyendo `AssignmentId`, `SectionId` (`SectionId`), `PlacedAt = UtcNow` y `PlacedByUserId = user_id`.
10. Obtiene la URL de redirección del QR desde `QrConfig.Clients[company_id]` (o vacío si no hay configuración) y genera:
    - QR con header `"VOUCHER DE ASIGNACIÓN"`.
    - Código de barras.
    - Inserta dos filas en `Codes` (una `Qr` y una `Bar`) asociadas a la asignación.
11. Setea `assignment.Status = AssignmentOperationalStatus.InProgress`, actualiza con `UpdateAsync` y guarda todos los cambios con `SaveChangesAsync`.
12. Devuelve `AssignMerchandiseDesignatedLocationDto` con los códigos generados.

> **Contexto de la request:** `company_id`, `module_code` y `operational_order_id` los inyecta `BaseAssignmentResourceController.AssignAsync` desde la ruta, y `user_id` lo lee el action de `HttpContext.Items["UserId"]`. `assignment_id` es el único que asigna el action, porque la command lo llama `AssignmentOperationalId`.

> **Validación (FluentValidation):** el `AssignMerchandiseDesignatedLocationValidator` valida que `sections` no esté vacío, que `section_id` sea requerido y distinto de `Guid.Empty`, que cada bloque (`tramos`/`racks`) tenga `block_id` y al menos una posición, que ningún `position_id` sea `Guid.Empty`, y que no haya `position_ids` repetidos (global, entre todas las secciones y bloques).

> **Re-ejecución:** la segunda ejecución con los mismos datos no se procesa porque la asignación ya quedó en `InProgress` (`ERP:ASSIGNMENT_IN_PROGRESS`). No se consulta el histórico de `assignment_stock_placements` para evitarlo; el control es por estado.

---

## Respuestas

### ✅ 200 OK

Devuelve un `AssignMerchandiseDesignatedLocationDto` con los códigos generados. El JSON se serializa con `SnakeCaseLower`.

```json
{
  "code_qr": "iVBORw0KGgoAAAANSUhEUg...",
  "code_bar": "6500000000012"
}
```

#### Campos

| Campo | Tipo | Descripción |
|---|---|---|
| `code_qr` | `string` | Contenido/imagen del código QR generado para el voucher de la asignación. |
| `code_bar` | `string` | Contenido del código de barras generado para el voucher de la asignación. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Estado de las posiciones | Deben estar `Available`; pasan a `Reserved` al asignarse. |
| Histórico | Se inserta una fila por posición en `assignment_stock_placements` con `SectionId`, `PlacedAt` y `PlacedByUserId`. |
| Códigos | Se generan dos registros en `Codes` por asignación (uno `Qr` y uno `Bar`). |
| Cambio de estado | La asignación pasa de `Pending` a `InProgress` al completar la asignación. |
| Almacén | Las secciones deben pertenecer al almacén (`WarehouseId`) de la asignación. |
| Re-ejecución | Bloqueada por estado: en la segunda ejecución responde `ERP:ASSIGNMENT_IN_PROGRESS` sin procesar. |
| Orden de validación de estado | `InProgress` se evalúa antes que `Pending`; cualquier estado distinto de `Pending` (salvo `InProgress`) responde `ERP:ASSIGNMENT_NOT_SENT`. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:POSITION_NOT_AVAILABLE",
    "description": "La posición RACK-01-N1P1 no está disponible."
  },
  "createdAt": "2026-10-07 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_IN_PROGRESS` | `La operación ya está en proceso/ejecución.` |
| `ERP:ASSIGNMENT_NOT_SENT` | `La asignación aún no ha sido enviada a descargar.` |
| `ERP:DUPLICATED_POSITIONS` | `No puede asignar la misma posición más de una vez.` |
| `ERP:SECTION_NOT_FOUND` | `Una o más secciones no fueron encontradas.` |
| `ERP:POSITION_SECTION_MISMATCH` | `Una o más secciones no pertenecen al almacén de la asignación.`, `La sección indicada no es una sección de tramos.`, `La sección indicada no es una sección de racks.`, `Uno o más tramos no pertenecen a la sección indicada.`, `Uno o más racks no pertenecen a la sección indicada.` |
| `ERP:POSITION_NOT_FOUND` | `La posición {position_id} no fue encontrada en el tramo indicado.` / `... no fue encontrada en el rack indicado.` |
| `ERP:POSITION_NOT_AVAILABLE` | `La posición {position_code} no está disponible.` |
| `Validation_Error` | Errores del `AssignMerchandiseDesignatedLocationValidator`: `Debe indicar al menos una sección con posiciones a asignar.`, `No puede asignar la misma posición más de una vez.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `El id del tramo es requerido.`, `El tramo debe indicar al menos una posición.`, `Un tramo contiene ids de posición no válidos.`, `El id del rack es requerido.`, `El rack debe indicar al menos una posición.`, `Un rack contiene ids de posición no válidos.` |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 404 Not Found

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_NOT_FOUND` | `No se encontró la asignación solicitada.` |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `Forbidden` | `x-api-key` ausente o incorrecta. Lo devuelve el `ApiKeyMiddleware` antes de llegar al controller. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** (fuera de `CoreException`) el `ExceptionMiddleware` responde en **PascalCase**:

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "Server_Error",
    "Description": "Error interno no controlado."
  },
  "CreatedAt": "2026-10-07 14:32:10"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Posiciones asignadas exitosamente y voucher generado. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | `x-api-key` inválida. |
| `404` | La asignación operativa no existe. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `RackStatus`

Aplica al estado de cada posición (y también al de tramos y racks).

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Available` | Lista y libre para asignar mercadería. **Requerido** para asignar. |
| `2` | `Occupied` | Tiene mercadería asignada actualmente. |
| `3` | `UnderMaintenance` | Fuera de servicio por mantenimiento. |
| `4` | `Blocked` | Inhabilitado por otra causa. |
| `5` | `Reserved` | Apartado para una operación en curso. **Estado resultante** tras asignar. |

### `SectionStorageType`

Determina qué tipo de bloque acepta cada sección.

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Racks` | Sección de racks: recibe bloques en `racks`. |
| `2` | `Lots` | Sección de tramos: recibe bloques en `tramos`. |
| `3` | `Pallets` | No soportado para este endpoint. |
| `4` | `None` | No soportado para este endpoint. |

### `AssignmentOperationalStatus`

Estado de la asignación operativa relevante para este endpoint.

| Valor | Nombre | Descripción |
|:---:|---|---|
| `0` | `None` | Sin estado. |
| `1` | `Pending` | Enviada a descargar; **permite** asignar posiciones. |
| `2` | `InProgress` | En proceso/ejecución; la asignación pasa a este estado tras asignar posiciones y bloquea la re-ejecución. |
| `3` | `OnHold` | En pausa; no permite asignar posiciones. |
| `4` | `Downloaded` | Descargada; no permite asignar posiciones. |

### `CodesType`

Tipo de código generado por asignación.

| Valor | Nombre | Descripción |
|:---:|---|---|
| `0` | `None` | Sin tipo. |
| `1` | `Qr` | Código QR del voucher. |
| `2` | `Bar` | Código de barras del voucher. |