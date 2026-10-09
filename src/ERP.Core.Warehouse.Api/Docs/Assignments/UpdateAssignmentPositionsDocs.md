# Asignaciones Operativas

## Actualizar Información de Polines

Endpoint para registrar la información de polines de una asignación operativa durante la descarga de un camión (rastreo de información de la descarga). Requiere que el `POST` de posiciones (`assignment-positions`) ya se haya ejecutado (la asignación debe estar en estado `InProgress`); en caso contrario responde `ERP:ASSIGNMENT_POST_REQUIRED`.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/assignment-positions` |
| **Descripción** | Declara el tipo de mercadería y la información de polines de la asignación: cantidad de polines por tipo, dimensiones y bultos por polín. |
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

| Parámetro            | Tipo   | Requerido | Descripción |
|----------------------|--------|:---------:|-------------|
| `merchandise_type`   | `enum (UnloadingMerchandiseType)` | Sí | Tipo de mercadería del camión: `Bulk` (1, granel) o `Armed` (2, armada/empolinada). |
| `pallets`            | `array`| Sí        | Registros de polines, uno por tipo de polín (máx. 2: estándar y/o sobredimensionado). |

### Campos de `pallets[]`

| Parámetro         | Tipo   | Requerido | Descripción |
|-------------------|--------|:---------:|-------------|
| `type`            | `enum (PalletType)` | Sí | Tipo de polín: `Standard` (1, 1×1.2 m) o `Oversized` (2, sobredimensionado). |
| `count_pallets`   | `int`  | Sí        | Cantidad de polines de ese tipo. Mayor que cero. |
| `width`           | `decimal` | No*    | * Sólo para `Oversized`: largo del polín (m), mayor que cero. En `Standard` se ignora: el servidor fuerza `1`. |
| `length`          | `decimal` | No*    | * Sólo para `Oversized`: ancho del polín (m), mayor que cero. En `Standard` se ignora: el servidor fuerza `1.2`. |
| `bulks_per_pallet`| `int`  | No*    | * Bultos por polín. **Requerido (> 0)** si `merchandise_type = Bulk`; **no aplica** si `merchandise_type = Armed` (se fuerza `null`). |

**Reglas:**
- Si es `Bulk`, se declara `bulks_per_pallet` en cada registro. Si la asignación contiene los dos tipos de polines, se envían **2 registros**, cada uno con su cantidad de bultos por polín.
- Si es `Armed`, solo se declara cantidad de polines, tipo y dimensiones (sin `bulks_per_pallet`).
- No se permite más de un registro por `type`.
- No valida los bultos contra el total declarado de la orden operativa (`PackagesCount`).

---

## Ejemplos del Body

### Mercadería granel con ambos tipos de polines

```json
{
  "merchandise_type": 1,
  "pallets": [
    { "type": 1, "count_pallets": 10, "width": 0, "length": 0, "bulks_per_pallet": 5 },
    { "type": 2, "count_pallets": 3, "width": 1.5, "length": 2.1, "bulks_per_pallet": 8 }
  ]
}
```

### Mercadería granel con un solo tipo de polín

```json
{
  "merchandise_type": 1,
  "pallets": [
    { "type": 1, "count_pallets": 12, "width": 0, "length": 0, "bulks_per_pallet": 5 }
  ]
}
```

### Mercadería armada/empolinada

```json
{
  "merchandise_type": 2,
  "pallets": [
    { "type": 1, "count_pallets": 8, "width": 0, "length": 0 },
    { "type": 2, "count_pallets": 2, "width": 1.6, "length": 2.0 }
  ]
}
```

---

## Flujo del Handler (`UpdateAssignmentPositionsHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code, ct)`: valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo. Si falla devuelve `400`.
2. Bloquea al rol `Supervisor` (`ERP:INVALID_ACCESS`).
3. Carga la asignación operativa (`Id == assignment_id AND OperationalOrderId == operational_order_id AND IsActive AND DeletedAt == null`). Si no existe: `ERP:ASSIGNMENT_NOT_FOUND`.
4. Si `assignment.Status != InProgress`: `ERP:ASSIGNMENT_POST_REQUIRED` ("Debe asignar las posiciones antes de registrar la información de polines."). Esto garantiza que el `POST` de posiciones ya se ejecutó.
5. Construye la lista de `PositionatingInformation` normalizada:
   - `Standard` → `width = 1`, `length = 1.2` (se ignora lo enviado en el body).
   - `Oversized` → usa `width`/`length` enviados.
   - `Armed` → `bulks_per_pallet = null`; `Bulk` → conserva `bulks_per_pallet`.
6. Setea `assignment.MerchandiseType = merchandise_type` y `assignment.HasPositionatingInformation = true`.
7. Serializa `AdditionalDataAssingmentOperational` en `assignment.AdditionalData` como JSON `snake_case` (mismo patrón que `UpdateReceptionEntrance`).
8. Actualiza la asignación y guarda (`SaveChangesAsync`). Responde `204 No Content`.

> **Validación (FluentValidation):** `UpdateAssignmentPositionsValidator` valida que `merchandise_type` sea un enum válido, `pallets` mínimo 1, sin `type` repetido, `count_pallets > 0`, `width`/`length > 0` para `Oversized`, `bulks_per_pallet > 0` para `Bulk` y `bulks_per_pallet = null` para `Armed`.

---

## Respuestas

### ✅ 204 No Content

Se registró la información de polines correctamente. Sin cuerpo de respuesta.

### ❌ 400 Bad Request

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_POST_REQUIRED` | `Debe asignar las posiciones antes de registrar la información de polines.` |
| `ERP:INVALID_ACCESS` | `No tienes acceso a realizar esta acción` |
| `Validation_Error` | Errores del `UpdateAssignmentPositionsValidator` (enum inválido, pallets vacíos, registros duplicados, dimensiones faltantes, bultos inválidos, etc.). |
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

## Catálogos de Enums Utilizados

### `UnloadingMerchandiseType`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Bulk` | Mercadería a granel: requiere `bulks_per_pallet` por registro. |
| `2` | `Armed` | Mercadería armada/empolinada: no aplica `bulks_per_pallet`. |

### `PalletType`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Standard` | Polín estándar: dimensiones fijas `1 × 1.2` m (server). |
| `2` | `Oversized` | Polín sobredimensionado: requiere `width`/`length` medidos. |