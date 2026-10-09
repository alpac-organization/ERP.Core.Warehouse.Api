# Asignaciones Operativas

## Asignar Posiciones e Información de Polines (PATCH)

Endpoint único que fusiona dos operaciones sobre una asignación operativa **en proceso de descarga** (estado `InProgress`):

- **Asignar posiciones de almacén** (tramos y/o racks): reserva las posiciones (`Available → Reserved`), registra el histórico en `assignment_stock_placements` y genera los códigos QR/barcode del voucher. Es el mismo flujo que antes exponía el `POST .../assignment-positions` y el `POST .../merchandise`.
- **Registrar/actualizar la información de polines** de la descarga: tipo de mercadería (`merchandise_type`) y pallets (`pallets`), con semántica de **upsert parcial** sobre `AdditionalData` (patrón `UpdateReceptionEntrance`).

Es un **PATCH parcial**: solo se procesa lo que llega en el body. Debe venir **al menos uno** de `sections`, `merchandise_type` o `pallets`. El endpoint **solo se consume** cuando la asignación está `InProgress`; el estado no cambia. Para llegar a `InProgress` primero debe ejecutarse `POST .../start-task` (que valida `Pending`).

> El `merchandise_type` puede venir en el mismo request que `sections` o de forma independiente.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/assignment-positions` |
| **Descripción** | Asigna posiciones y/o actualiza el tipo de mercadería y la información de polines de la asignación. |
| **Tags**        | `Asignaciones de posiciones` |

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

Debe venir **al menos uno** de los tres grupos. Los campos no enviados no se procesan.

| Parámetro         | Tipo   | Requerido | Descripción |
|-------------------|--------|:---------:|-------------|
| `sections`        | `array`| No        | Posiciones a asignar (tramos y/o racks). Si viene, ejecuta el flujo completo de asignación de posiciones (reserva + histórico + códigos). |
| `merchandise_type`| `enum (UnloadingMerchandiseType)` | No | Tipo de mercadería del camión: `Bulk` (1, granel) o `Armed` (2, armada/empolinada). |
| `pallets`         | `array`| No        | Registros de polines a agregar/actualizar/eliminar (upsert por `type`). Máx. 1 registro por tipo de polín. |

### Campos de `sections[]`

| Parámetro                   | Tipo      | Requerido | Descripción |
|-----------------------------|-----------|:---------:|-------------|
| `sections[].section_id`     | `guid`    | Sí        | Id de la sección. |
| `sections[].tramos`         | `array`   | No        | Bloques de tramos de la sección. Solo válido si la sección es de tipo `Lots`. |
| `sections[].tramos[].block_id` | `guid` | Sí        | Id del tramo (entidad `Lots`). |
| `sections[].tramos[].position_ids` | `guid[]` | Sí   | Ids de las posiciones del tramo a asignar. Mínimo uno. |
| `sections[].racks`           | `array`   | No        | Bloques de racks de la sección. Solo válido si la sección es de tipo `Racks`. |
| `sections[].racks[].block_id` | `guid`   | Sí        | Id del rack (entidad `Racks`). |
| `sections[].racks[].position_ids` | `guid[]` | Sí    | Ids de las posiciones a asignar. Mínimo una. |

### Campos de `pallets[]`

Semántica de **upsert por `type`**: si el `type` ya existe, se actualizan únicamente los campos informados.

| Parámetro         | Tipo   | Requerido | Descripción |
|-------------------|--------|:---------:|-------------|
| `type`            | `enum (PalletType)` | Sí | Referencia el tipo de polín: `Standard` (1, 1×1.2 m) o `Oversized` (2, sobredimensionado). |
| `delete`          | `bool` | No (default `false`) | Si `true`, elimina el registro de ese `type` del estado actual e ignora el resto de campos. |
| `count_pallets`   | `int`  | No*    | * Si se informa: mayor que cero. Si se omite conserva el valor existente. |
| `width`           | `decimal` | No* | * Solo aplica a `Oversized`; si se informa: mayor que cero. En `Standard` se ignora: el servidor fuerza `1`. |
| `length`          | `decimal` | No* | * Solo aplica a `Oversized`; si se informa: mayor que cero. En `Standard` se ignora: el servidor fuerza `1.2`. |
| `bulks_per_pallet`| `int`  | No*    | * Bultos por polín. Si se informa: mayor que cero. Si `merchandise_type` final es `Armed` se fuerza `null`. |

**Reglas de consistencia de polines (validadas por el handler tras el merge):**
- Cada registro debe quedar con `count_pallets > 0`.
- Cada `Oversized` debe quedar con `width > 0` y `length > 0`.
- Si el `merchandise_type` final es `Bulk`, **todos** los registros deben quedar con `bulks_per_pallet > 0` (`ERP:INVALID_POSITIONATING_INFORMATION`).
- Si el `merchandise_type` final es `Armed`, todos los `bulks_per_pallet` se limpian a `null` automáticamente.
- Al crear un registro nuevo con `merchandise_type = Bulk`, `bulks_per_pallet` es obligatorio en ese registro (`ERP:INVALID_BULKS_PER_PALLET`).
- No valida los bultos contra el total declarado de la orden operativa (`PackagesCount`).

---

## Ejemplos del Body

### Asignar posiciones + declarar mercadería granel con ambos tipos de polines

```json
{
  "merchandise_type": 1,
  "pallets": [
    { "type": 1, "count_pallets": 10, "bulks_per_pallet": 5 },
    { "type": 2, "count_pallets": 3, "width": 1.5, "length": 2.1, "bulks_per_pallet": 8 }
  ],
  "sections": [
    {
      "section_id": "f8a964a3-76a0-4fc7-bf98-251f28b4d081",
      "tramos": [
        {
          "block_id": "e2a48b32-0001-4444-8888-abcdef012345",
          "position_ids": [
            "3b2e591c-1111-4444-9999-012345abcdef",
            "4c3f692d-2222-4444-aaaa-123456789abc"
          ]
        }
      ],
      "racks": []
    }
  ]
}
```

### Solo posiciones (equivalente al antiguo POST de posiciones / POST /merchandise)

```json
{
  "sections": [
    {
      "section_id": "f8a964a3-76a0-4fc7-bf98-251f28b4d081",
      "tramos": [
        {
          "block_id": "e2a48b32-0001-4444-8888-abcdef012345",
          "position_ids": [
            "3b2e591c-1111-4444-9999-012345abcdef"
          ]
        }
      ],
      "racks": []
    }
  ]
}
```

### Solo información de polines (mercaduría armada)

```json
{
  "merchandise_type": 2,
  "pallets": [
    { "type": 1, "count_pallets": 8 },
    { "type": 2, "count_pallets": 2, "width": 1.6, "length": 2.0 }
  ]
}
```

### Actualización parcial: solo cambiar la cantidad del polín estándar

```json
{
  "pallets": [
    { "type": 1, "count_pallets": 12 }
  ]
}
```

### Eliminar el tipo sobredimensionado

```json
{
  "pallets": [
    { "type": 2, "delete": true }
  ]
}
```

---

## Flujo del Handler (`AssignMerchandiseDesignatedLocationHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code, ct)`: valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo. Si falla devuelve `400`.
2. Bloquea al rol `Supervisor` (`ERP:INVALID_ACCESS`).
3. Carga la asignación operativa (`Id == assignment_id AND OperationalOrderId == operational_order_id AND IsActive AND DeletedAt == null`). Si no existe: `ERP:ASSIGNMENT_NOT_FOUND`.
4. Si `assignment.Status != InProgress`: `ERP:ASSIGNMENT_NOT_IN_PROGRESS` ("La asignación no está en proceso."). El endpoint **no se consume** fuera de `InProgress`; para llegar a ese estado debe ejecutarse previamente `POST .../start-task`.
5. **Si `sections` viene** → flujo de asignación de posiciones:
   - Recoge todos los `position_ids`; si hay repetidos (global, entre todos los bloques): `ERP:DUPLICATED_POSITIONS`.
   - Consulta `Sections` en un solo query (`AsSingleQuery`); si no se encuentran todas: `ERP:SECTION_NOT_FOUND`.
   - Por sección valida: pertenencia al almacén de la asignación, tipo de almacenamiento (`Lots`/`Racks`), pertenencia de tramos/racks y que cada posición esté `Available` (`ERP:POSITION_SECTION_MISMATCH`, `ERP:POSITION_NOT_FOUND`, `ERP:POSITION_NOT_AVAILABLE`).
   - Reserva: `Position.Status = Reserved` + histórico en `assignment_stock_placements` (`AssignStockPlacement`).
   - Genera códigos QR/barcode (URL de redirección desde `QrConfig`, `logo = company.ImageUrl`), inserta en `Codes` (uno `Qr` y uno `Bar`) y guarda los códigos para la respuesta.
   - **No cambia** el estado (la asignación ya está `InProgress`).
6. **Si `merchandise_type` o `pallets` vienen** → merge de polines:
   - **Deserializa** `assignment.AdditionalData` en `AdditionalDataAssingmentOperational` (patrón `DeserializeAdditionalData`); si no hay datos, parte de una lista vacía.
   - Actualiza `MerchandiseType` si viene (antes del merge, para que las reglas de granel usen el tipo final).
   - **Upsert por `type`**: `delete = true` → elimina el tipo; si el `type` no existe lo agrega (en granel exige `bulks_per_pallet > 0` desde el registro); si existe actualiza solo los campos informados.
   - `Standard` → fuerza `width = 1`, `length = 1.2` (ignora lo enviado). `Oversized` → conserva/provee `width`/`length`.
   - `Bulk` → guarda `bulks_per_pallet` cuando se envía. `Armed` → fuerza `bulks_per_pallet = null` en todos los registros.
   - Valida la consistencia final (reglas arriba): si falla, `ERP:INVALID_POSITIONATING_INFORMATION`.
   - Setea `HasPositionatingInformation = count de registros > 0` y serializa `AdditionalData` como JSON `snake_case`.
7. `SaveChangesAsync` **único al final**: si alguna validación falla en pasos anteriores, **nada** se persiste.
8. Responde:
   - Si se asignaron posiciones → `200` con `AssignMerchandiseDesignatedLocationDto` (URLs/imágenes de los códigos QR y de barras).
   - Si solo se actualizó información de polines → `204 No Content`.

> **Validación (FluentValidation):** `AssignMerchandiseDesignatedLocationValidator` valida que venga al menos uno de `sections`, `merchandise_type` o `pallets`; enum válidos; `pallets` sin `type` repetido y con campos informados válidos; y si `sections` viene, las reglas de sección/tramo/rack y sin posiciones duplicadas. La consistencia post-merge se valida en el handler.

---

## Respuestas

### ✅ 200 OK

Solo cuando se asignaron posiciones (vino `sections`). Devuelve un `AssignMerchandiseDesignatedLocationDto` con la URL/imagen de los códigos generados. El JSON se serializa con `SnakeCaseLower`.

```json
{
  "code_qr": "iVBORw0KGgoAAAANSUhEUg...",
  "code_bar": "6500000000012"
}
```

| Campo | Tipo | Descripción |
|---|---|---|
| `code_qr` | `string` | URL/imagen del código QR generado para el voucher de la asignación. |
| `code_bar` | `string` | Contenido del código de barras generado para el voucher de la asignación. |

### ✅ 204 No Content

Solo si se actualizó información de polines (vino `merchandise_type` y/o `pallets`) **sin** asignar posiciones. Sin cuerpo de respuesta.

### ❌ 400 Bad Request

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_NOT_IN_PROGRESS` | `La asignación no está en proceso.` |
| `ERP:INVALID_BULKS_PER_PALLET` | `Debe indicar la cantidad de bultos por polín para mercadería a granel.` |
| `ERP:INVALID_POSITIONATING_INFORMATION` | `La información de polines no es coherente con el tipo de mercadería.` |
| `ERP:DUPLICATED_POSITIONS` | `No puede asignar la misma posición más de una vez.` |
| `ERP:SECTION_NOT_FOUND` | `Una o más secciones no fueron encontradas.` |
| `ERP:POSITION_SECTION_MISMATCH` | `Una o más secciones no pertenecen al almacén de la asignación.`, `La sección indicada no es una sección de tramos.`, `La sección indicada no es una sección de racks.`, `Uno o más tramos no pertenecen a la sección indicada.`, `Uno o más racks no pertenecen a la sección indicada.` |
| `ERP:POSITION_NOT_FOUND` | `La posición {position_id} no fue encontrada en el tramo indicado.` / `... no fue encontrada en el rack indicado.` |
| `ERP:POSITION_NOT_AVAILABLE` | `La posición {position_code} no está disponible.` |
| `ERP:INVALID_ACCESS` | `No tienes acceso a realizar esta acción` |
| `Validation_Error` | Errores del `AssignMerchandiseDesignatedLocationValidator`. |
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
  "CreatedAt": "2026-10-08 14:32:10"
}
```

---

## Notas y compatibilidad

| Endpoint | Estado actual |
|---|---|
| `PATCH .../assignment-positions` | **Único endpoint** del flujo fusionado (este). |
| `POST .../assignment-positions` | **Eliminado.** Su lógica quedó fusionada en este PATCH. |
| `POST .../merchandise` (`AssignmentMerchandiseController`) | **Conservado** (otros servicios lo consumen). Comparte el mismo handler: sigue recibiendo `sections`, responde el mismo DTO con códigos, pero ahora exige `InProgress` (debe llamarse `start-task` previamente). |

---

## Catálogos de Enums Utilizados

### `UnloadingMerchandiseType`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Bulk` | Mercadería a granel: cada registro requiere `bulks_per_pallet > 0`. |
| `2` | `Armed` | Mercadería armada/empolinada: no aplica `bulks_per_pallet` (se fuerza `null`). |

### `PalletType`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Standard` | Polín estándar: dimensiones fijas `1 × 1.2` m (server). |
| `2` | `Oversized` | Polín sobredimensionado: requiere `width`/`length` medidos. |

### `AssignmentOperationalStatus`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `0` | `None` | Sin estado. |
| `1` | `Pending` | Enviada a descargar; permite iniciar la tarea. |
| `2` | `InProgress` | En proceso/ejecución; **requerido** para consumir este PATCH. |
| `3` | `OnHold` | En pausa; no permite consumir este PATCH. |
| `4` | `Downloaded` | Descargada; no permite consumir este PATCH. |