# Tramos

## Obtener Coordenadas de Tramo

Endpoint para consultar las coordenadas cartesianas de un tramo dentro de una sección de un almacén. Sirve para posicionar el tramo en el render 2D del frontend.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}/coordinates` |
| **Descripción** | Retorna la posición (X, Y, Z) y la rotación sobre el eje Y del tramo. Si el tramo aún no tiene coordenadas, devuelve los valores por defecto en lugar de un `404`. |
| **Tags**        | `Coordenadas de Tramos` |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:-------------:|:--------:|:---------:|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid`   | Sí        | Identificador único del almacén. |
| `sections_id`  | `guid`   | Sí        | Identificador único de la sección. |
| `lot_id`       | `guid`   | Sí        | Identificador único del tramo. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `X-Api-Key`     | `{api_key}`      | Sí        |

---

## Flujo del Handler (`GetLotCoordinatesHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Busca la sección con `Id == sections_id AND IsActive AND DeletedAt == null`. Si no existe: `ERP:SECTION_NOT_FOUND`.
3. Verifica que `section.WarehouseId == warehouse_id`. Si no: `ERP:SECTION_WAREHOUSE_MISMATCH`.
4. Busca el tramo con `Id == lot_id AND SectionId == sections_id AND DeletedAt == null`, con `Include(LotsCoordinates)`. Si no existe: `ERP:LOT_NOT_FOUND`.
5. Si `lot.LotsCoordinates is null` devuelve un `LotCoordinatesDto` con `LotId = lot.Id` y el resto en `0`, y **no** un error.
6. Si existen coordenadas, mapea `LotsCoordinates` a `LotCoordinatesDto` con `LotsProfile` y lo devuelve.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y `warehouse_id`, `sections_id` y `lot_id` los asigna el action.

> **Validación (FluentValidation):** el `GetLotCoordinatesValidator` valida `warehouse_id` requerido, `sections_id` requerido y distinto de `Guid.Empty`, y `lot_id` requerido y distinto de `Guid.Empty`.

> Este endpoint usa `AsNoTracking` porque es de solo lectura y la consulta se materializa para leer la navegación `LotsCoordinates`.

---

## Respuestas

### ✅ 200 OK

Devuelve un `LotCoordinatesDto`. El JSON se serializa con `SnakeCaseLower`.

```json
{
  "lot_id": "56487c1b-9f4d-4b2a-8e1c-1234567890ab",
  "position_x": 1.25,
  "position_y": 0.5,
  "position_z": 0,
  "rotation_y": 90
}
```

| Campo        | Tipo      | Descripción |
|--------------|-----------|-------------|
| `lot_id`     | `guid`    | Id del tramo. |
| `position_x` | `decimal` | Posición X en metros desde el origen de la sección. |
| `position_y` | `decimal` | Posición Y en metros desde el origen de la sección. |
| `position_z` | `decimal` | Posición Z en metros. Normalmente `0`. |
| `rotation_y` | `decimal` | Rotación en grados sobre el eje Y. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Tramo sin coordenadas | Un tramo recién creado no tiene fila en `lot_coordinates`. En ese caso la respuesta es `200` con `position_x`, `position_y`, `position_z` y `rotation_y` en `0`. **Nunca** devuelve `404` por falta de coordenadas. |
| Origen de las coordenadas | `position_x` y `position_y` se miden en metros desde el origen `(0,0)` de la sección, no del almacén. |
| Valores admitidos | El backend no restringe el signo: puede devolver valores negativos si fueron guardados como tales. |
| Precisión | La columna es `numeric(18,6)`, admite hasta 6 decimales. |
| Rotación | `rotation_y` es un decimal libre. No está restringido a `0`, `90`, `180` o `270`. |
| Relación 1:1 | Un tramo tiene como máximo una fila en `lot_coordinates`, garantizado por el índice único `ux_lot_coordinates_lot_id`. |
| Tramo dado de baja | Un tramo con `DeletedAt` informado responde `ERP:LOT_NOT_FOUND`. |
| Capacidades | Este endpoint no lee `lots_capacity` ni `section_capacity`. No valida límites ni necesita que existan las capacidades. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:LOT_NOT_FOUND",
    "description": "El tramo no fue encontrado."
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:LOT_NOT_FOUND` | `El tramo no fue encontrado.` |
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | `La sección no pertenece al almacén indicado.` |
| `Validation_Error` | Errores del `GetLotCoordinatesValidator`: `El id del almacén es requerido.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `El id del tramo es requerido.`, `El id del tramo no es válido.` |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
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
  "CreatedAt": "2026-09-30 14:32:10"
}
```

---

## Códigos de Estado

| Código | Descripción |
|--------|-------------|
| `200` | Coordenadas obtenidas, o valores por defecto `0,0,0,0` si el tramo aún no tiene fila en `lot_coordinates`. |
| `400` | Error de validación, acceso o si el tramo/sección no existe (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Endpoints relacionados

| Método | Endpoint | Uso |
|--------|----------|-----|
| `POST` | `.../lots/{lot_id}/coordinates` | Registra las coordenadas por primera vez. |
| `PATCH` | `.../lots/{lot_id}/coordinates` | Reposiciona un tramo que ya tiene coordenadas. |