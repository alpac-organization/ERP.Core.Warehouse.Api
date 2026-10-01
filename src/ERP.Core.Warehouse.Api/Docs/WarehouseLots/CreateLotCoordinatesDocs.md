# Tramos

## Registrar Coordenadas de Tramo

Endpoint para crear las coordenadas cartesianas de un tramo por primera vez. Escribe una fila en `lot_coordinates` para el `lot_id` indicado.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}/coordinates` |
| **Descripción** | Registra la posición (X, Y, Z) y la rotación sobre el eje Y de un tramo que todavía no tiene coordenadas. |
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

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `X-Api-Key`     | `{api_key}`        | Sí        |
| `Content-Type`  | `application/json` | Sí        |

---

## Request Body

`company_id`, `module_code`, `warehouse_id`, `sections_id`, `lot_id` y `user_id` no forman parte del JSON: se toman de la ruta y del token (`[JsonIgnore]`).

**Todos los campos son opcionales.** Los que se omitan o lleguen en `null` se guardan en `0`.

| Parámetro    | Tipo      | Requerido | Default | Descripción |
|--------------|-----------|:---------:|---------|-------------|
| `position_x` | `decimal` | No        | `0`     | Posición X en metros desde el origen de la sección. |
| `position_y` | `decimal` | No        | `0`     | Posición Y en metros desde el origen de la sección. |
| `position_z` | `decimal` | No        | `0`     | Posición Z en metros. Normalmente `0`. |
| `rotation_y` | `decimal` | No        | `0`     | Rotación en grados sobre el eje Y. |

```json
{
  "position_x": 1.25,
  "position_y": 0.5,
  "position_z": 0,
  "rotation_y": 90
}
```

| Campo / regla | Descripción |
|---|---|
| Valores por defecto | Un campo ausente o `null` se persiste como `0`. Un body `{}` crea la fila en `0,0,0,0`. |
| Precisión | La columna es `numeric(18,6)`, admite hasta 6 decimales. No hay validación de escala en el request. |
| Signo | No se restringe a valores positivos. Se aceptan negativos. |
| Rotación | No está restringida a `0`, `90`, `180` o `270`. Es un decimal libre. |
| Origen de las coordenadas | `position_x` y `position_y` se miden en metros desde el origen `(0,0)` de la sección, no del almacén. |

---

## Flujo del Handler (`CreateLotCoordinatesHandler`)

1. `ValidateAccessAndGetSectionAsync` (heredado de `BaseLotsCapacityHandler`): valida acceso, busca la sección con `IsActive AND DeletedAt == null` y verifica que pertenezca al almacén. Errores: `ERP:SECTION_NOT_FOUND`, `ERP:SECTION_WAREHOUSE_MISMATCH`.
2. `GetExistingLotAsync(lot_id, sections_id)` (heredado): busca el tramo con `DeletedAt == null` e incluye `LotsCapacity`, `Positions` y `LotsCoordinates`. Si no existe: `ERP:LOT_NOT_FOUND`.
3. Si `lot.LotsCoordinates is not null` → `ERP:LOT_COORDINATES_ALREADY_EXIST`.
4. Calcula los valores efectivos: `positionX = request.PositionX ?? 0` y `positionY = request.PositionY ?? 0`.
5. `ValidateLotPlacement(lot, section, positionX, positionY)` (heredado de `BaseLotsCapacityHandler`) comprueba el emplazamiento contra las dimensiones de la sección. Ver la sección de límites más abajo.
6. Construye la entidad con `LotsProfile.ToLotCoordinatesEntity(request, lot.Id)`: `Id = Guid.NewGuid()`, `LotId`, y los cuatro valores con sus defaults.
7. `await _unitOfWork.LotCoordinates.RegisterLotCoordinate(entity)`.
8. `await _unitOfWork.SaveChangesAsync(cancellationToken)`.
9. Retorna `true` y el controller responde `200 OK`.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y `warehouse_id`, `sections_id` y `lot_id` los asigna el action sobre la command.

> **Validación (FluentValidation):** el `CreateLotCoordinatesValidator` valida **únicamente** los tres identificadores de ruta: `warehouse_id`, `sections_id` y `lot_id` requeridos y distintos de `Guid.Empty`. No hay reglas sobre `position_x`, `position_y`, `position_z` ni `rotation_y`.

---

## Validación de emplazamiento

`ValidateLotPlacement` se ejecuta en el handler, no en el validator, porque necesita leer `section.SectionCapacity.Width/Length` y `lot.LotsCapacity.Width/Length` desde la base de datos.

| Caso | `typeError` |
|------|-------------|
| La sección no tiene fila en `section_capacity` | `ERP:SECTION_CAPACITY_NOT_FOUND` |
| El tramo no tiene fila en `lots_capacity` | `ERP:LOT_CAPACITY_NOT_FOUND` |
| `positionX + lot.LotsCapacity.Width > section.SectionCapacity.Width` | `ERP:LOT_OUTSIDE_SECTION_BOUNDS` |
| `positionY + lot.LotsCapacity.Length > section.SectionCapacity.Length` | `ERP:LOT_OUTSIDE_SECTION_BOUNDS` |

> Solo se evalúa el **límite superior** de cada eje. No hay cota inferior: una posición negativa no se rechaza por signo, siempre que la suma con el ancho o largo del tramo no supere las dimensiones de la sección.

> Las capacidades se miden con 2 decimales (`numeric(18,2)`) mientras que las coordenadas admiten 6. Si el frontend calcula el límite con precisión mayor a 2 decimales puede obtener un rechazo por `ERP:LOT_OUTSIDE_SECTION_BOUNDS` en un caso límite.

---

## Respuestas

### ✅ 200 OK

Registro exitoso. El controller retorna `OkResult`, por lo que la respuesta es `200` sin cuerpo.

### Notas

| Campo / regla | Descripción |
|---|---|
| Tramo ya posicionado | Si el tramo ya tiene fila en `lot_coordinates` la respuesta es `ERP:LOT_COORDINATES_ALREADY_EXIST`. Para reposicionar hay que usar el `PATCH`, no volver a hacer `POST`. |
| Relación 1:1 | El índice único `ux_lot_coordinates_lot_id` garantiza un solo registro por tramo. |
| Dependencia de capacidades | Para validar el emplazamiento es necesario que existan `section_capacity` y `lots_capacity`. Un tramo recién registrado ya tiene `lots_capacity`; la capacidad de sección se crea con el propio `POST` de tramos. |
| No recalcula áreas | Este endpoint no toca `section_capacity` ni `warehouse_capacity` y no recalcula m². Mover un tramo no altera las áreas ni los porcentajes de disponibilidad. |
| Todo o nada | La inserción se persiste en una única transacción. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:LOT_COORDINATES_ALREADY_EXIST",
    "description": "El tramo ya tiene coordenadas registradas. Utilice el endpoint de actualización."
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:LOT_COORDINATES_ALREADY_EXIST` | `El tramo ya tiene coordenadas registradas. Utilice el endpoint de actualización.` |
| `ERP:LOT_OUTSIDE_SECTION_BOUNDS` | `La posición del tramo excede las dimensiones de la sección.` |
| `ERP:LOT_CAPACITY_NOT_FOUND` | `No se encontró la capacidad del tramo para validar su ubicación.` |
| `ERP:SECTION_CAPACITY_NOT_FOUND` | `La sección no tiene capacidad registrada para recalcular.` |
| `ERP:LOT_NOT_FOUND` | `El tramo no fue encontrado.` |
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | `La sección no pertenece al almacén indicado.` |
| `Validation_Error` | Errores del `CreateLotCoordinatesValidator`: `El id del almacén es requerido.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `El id del tramo es requerido.`, `El id del tramo no es válido.` |
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
| `200` | Coordenadas registradas exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Endpoints relacionados

| Método | Endpoint | Uso |
|--------|----------|-----|
| `GET` | `.../lots/{lot_id}/coordinates` | Lee las coordenadas; devuelve `0,0,0,0` si no existen. |
| `PATCH` | `.../lots/{lot_id}/coordinates` | Reposiciona un tramo que ya tiene coordenadas. |