# Tramos

## Actualizar Coordenadas de Tramo

Endpoint para reposicionar un tramo que ya tiene coordenadas registradas. Acepta actualización parcial: solo se aplican los campos que lleguen en el body.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}/coordinates` |
| **Descripción** | Actualiza la posición (X, Y, Z) y/o la rotación sobre el eje Y de un tramo ya posicionado. |
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

**Todos los campos son opcionales.** Un campo ausente o `null` conserva el valor persistido.

| Parámetro    | Tipo      | Requerido | Default | Descripción |
|--------------|-----------|:---------:|---------|-------------|
| `position_x` | `decimal` | No        | —       | Nueva posición X en metros desde el origen de la sección. |
| `position_y` | `decimal` | No        | —       | Nueva posición Y en metros desde el origen de la sección. |
| `position_z` | `decimal` | No        | —       | Nueva posición Z en metros. |
| `rotation_y` | `decimal` | No        | —       | Nueva rotación en grados sobre el eje Y. |

**Ejemplo — reposicionar solo el eje X** (deja `position_y` y `rotation_y` intactos):

```json
{
  "position_x": 4.75
}
```

**Ejemplo — rotar un tramo sin moverlo:**

```json
{
  "rotation_y": 180
}
```

| Campo / regla | Descripción |
|---|---|
| Actualización parcial | Solo se escriben los campos con valor. El resto conserva lo que ya estaba en `lot_coordinates`. |
| Body vacío | Un body `{}` revalida el emplazamiento actual y persiste sin cambios. |
| Precisión | La columna es `numeric(18,6)`, admite hasta 6 decimales. No hay validación de escala en el request. |
| Signo | No se restringe a valores positivos. Se aceptan negativos. |
| Rotación | No está restringida a `0`, `90`, `180` o `270`. Es un decimal libre. |
| Origen de las coordenadas | `position_x` y `position_y` se miden en metros desde el origen `(0,0)` de la sección, no del almacén. |

---

## Flujo del Handler (`PatchLotCoordinatesHandler`)

1. `ValidateAccessAndGetSectionAsync` (heredado de `BaseLotsCapacityHandler`): valida acceso, busca la sección con `IsActive AND DeletedAt == null` y verifica que pertenezca al almacén. Errores: `ERP:SECTION_NOT_FOUND`, `ERP:SECTION_WAREHOUSE_MISMATCH`.
2. `GetExistingLotAsync(lot_id, sections_id)` (heredado): busca el tramo con `DeletedAt == null` e incluye `LotsCapacity`, `Positions` y `LotsCoordinates`. Si no existe: `ERP:LOT_NOT_FOUND`.
3. Si `lot.LotsCoordinates is null` → `ERP:LOT_COORDINATES_NOT_FOUND`. Este endpoint **no crea** la fila: para el primer registro hay que usar el `POST`.
4. Aplica solo los campos presentes: `if (request.PositionX.HasValue) coordinates.PositionX = request.PositionX.Value`, y así con `PositionY`, `PositionZ` y `RotationY`.
5. `ValidateLotPlacement(lot, section, coordinates.PositionX, coordinates.PositionY)` (heredado de `BaseLotsCapacityHandler`) valida el emplazamiento **resultante**, usando los valores ya combinados y no solo los enviados. Ver la sección de límites más abajo.
6. `await _unitOfWork.LotCoordinates.UpdateAsync(coordinates)`.
7. `await _unitOfWork.SaveChangesAsync(cancellationToken)`.
8. Retorna `true` y el controller responde `200 OK`.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y `warehouse_id`, `sections_id` y `lot_id` los asigna el action sobre la command.

> **Validación (FluentValidation):** el `PatchLotCoordinatesValidator` valida **únicamente** los tres identificadores de ruta: `warehouse_id`, `sections_id` y `lot_id` requeridos y distintos de `Guid.Empty`. No hay reglas sobre `position_x`, `position_y`, `position_z` ni `rotation_y`.

> El paso 5 es importante: si el frontend manda solo `rotation_y`, la validación de límites se hace contra el `position_x` y `position_y` que ya estaban persistidos. No basta con validar únicamente lo recibido.

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

Actualización exitosa. El controller retorna `OkResult`, por lo que la respuesta es `200` sin cuerpo.

### Notas

| Campo / regla | Descripción |
|---|---|
| Requiere_coordinates previas | Un tramo sin fila en `lot_coordinates` responde `ERP:LOT_COORDINATES_NOT_FOUND`. Este endpoint no hace upsert. |
| Rotación libre | Como no hay cota ni catálogo de ángulos, se puede dejar el tramo en cualquier orientación. Si el frontend necesita restringirlo a `0/90/180/270`, debe hacerlo del lado del cliente o agregar una regla al validator. |
| Validación sobre el resultado | Los límites se evalúan con la posición final del tramo, no con la parcial del body. |
| No recalcula áreas | Este endpoint no toca `section_capacity` ni `warehouse_capacity` y no recalcula m². Mover un tramo no altera las áreas ni los porcentajes de disponibilidad. |
| Todo o nada | La actualización se persiste en una única transacción. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:LOT_COORDINATES_NOT_FOUND",
    "description": "El tramo no tiene coordenadas registradas. Utilice el endpoint de registro."
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:LOT_COORDINATES_NOT_FOUND` | `El tramo no tiene coordenadas registradas. Utilice el endpoint de registro.` |
| `ERP:LOT_OUTSIDE_SECTION_BOUNDS` | `La posición del tramo excede las dimensiones de la sección.` |
| `ERP:LOT_CAPACITY_NOT_FOUND` | `No se encontró la capacidad del tramo para validar su ubicación.` |
| `ERP:SECTION_CAPACITY_NOT_FOUND` | `La sección no tiene capacidad registrada para recalcular.` |
| `ERP:LOT_NOT_FOUND` | `El tramo no fue encontrado.` |
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | `La sección no pertenece al almacén indicado.` |
| `Validation_Error` | Errores del `PatchLotCoordinatesValidator`: `El id del almacén es requerido.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `El id del tramo es requerido.`, `El id del tramo no es válido.` |
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
| `200` | Coordenadas actualizadas exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Endpoints relacionados

| Método | Endpoint | Uso |
|--------|----------|-----|
| `GET` | `.../lots/{lot_id}/coordinates` | Lee las coordenadas; devuelve `0,0,0,0` si no existen. |
| `POST` | `.../lots/{lot_id}/coordinates` | Registra las coordenadas por primera vez. |