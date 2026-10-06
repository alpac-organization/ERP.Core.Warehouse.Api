# Secciones

## Registrar Coordenadas de Posiciones

Endpoint para registrar las coordenadas cartesianas (X, Y, Z, RotationY) de posiciones de tramos (`LotsPositions`) o posiciones de racks (`RackPositions`) en lote. Permite registrar coordenadas para múltiples posiciones en una sola petición.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/coordinates` |
| **Descripción** | Registra coordenadas para posiciones de tramos o posiciones de racks pertenecientes a una sección. |
| **Tags**        | `Coordenadas de Posiciones` |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:-------------:|:--------:|:---------:|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid`   | Sí        | Identificador único del almacén. |
| `section_id`   | `guid`   | Sí        | Identificador único de la sección. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `X-Api-Key`     | `{api_key}`        | Sí        |
| `Content-Type`  | `application/json` | Sí        |

---

## Request Body

Los campos `company_id`, `module_code`, `warehouse_id`, `section_id` y `user_id` no forman parte del JSON: se toman de la ruta y del token (`[JsonIgnore]`).

| Parámetro                     | Tipo                 | Requerido | Descripción |
|-------------------------------|----------------------|:---------:|-------------|
| `targetType`                  | `integer` (enum)     | Sí        | Tipo de coordenadas a registrar: `1` = LotsPositions, `2` = RackPositions. |
| `lotsPositionsInformation`    | `array`              | Condicional\* | Lista de coordenadas para posiciones de tramos. Requerido si `targetType = 1`. |
| `rackPositionsInformation`    | `array`              | Condicional\* | Lista de coordenadas para posiciones de racks. Requerido si `targetType = 2`. |

\* Al menos uno de los dos arrays debe tener elementos según el `targetType` seleccionado.

### Estructura de `LotsPositionsInformation`

| Parámetro       | Tipo      | Requerido | Descripción |
|-----------------|-----------|:---------:|-------------|
| `lotPositionId` | `guid`    | Sí        | Identificador único de la posición del tramo. |
| `positionX`     | `decimal` | Sí        | Posición X en metros desde el origen de la sección. |
| `positionY`     | `decimal` | Sí        | Posición Y en metros desde el origen de la sección. |
| `positionZ`     | `decimal` | Sí        | Posición Z en metros. Normalmente `0`. |
| `rotationY`     | `decimal` | Sí        | Rotación en grados sobre el eje Y (0-360). |

### Estructura de `RackPositionsInformation`

| Parámetro        | Tipo      | Requerido | Descripción |
|------------------|-----------|:---------:|-------------|
| `rackPositionId` | `guid`    | Sí        | Identificador único de la posición del rack. |
| `positionX`      | `decimal` | Sí        | Posición X en metros desde el origen de la sección. |
| `positionY`      | `decimal` | Sí        | Posición Y en metros desde el origen de la sección. |
| `positionZ`      | `decimal` | Sí        | Posición Z en metros. Normalmente `0`. |
| `rotationY`      | `decimal` | Sí        | Rotación en grados sobre el eje Y (0-360). |

### Ejemplo para LotsPositions (`targetType: 1`)

```json
{
  "targetType": 1,
  "lotsPositionsInformation": [
    {
      "lotPositionId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "positionX": 1.25,
      "positionY": 0.5,
      "positionZ": 0,
      "rotationY": 90
    },
    {
      "lotPositionId": "b2c3d4e5-f6a7-8901-bcde-f23456789012",
      "positionX": 2.5,
      "positionY": 1.0,
      "positionZ": 0,
      "rotationY": 0
    }
  ],
  "rackPositionsInformation": []
}
```

### Ejemplo para RackPositions (`targetType: 2`)

```json
{
  "targetType": 2,
  "lotsPositionsInformation": [],
  "rackPositionsInformation": [
    {
      "rackPositionId": "c3d4e5f6-a7b8-9012-cdef-345678901234",
      "positionX": 0.5,
      "positionY": 0.5,
      "positionZ": 0,
      "rotationY": 180
    }
  ]
}
```

---

## Flujo del Handler (`RegisterCoordinatesHandler`)

1. **Validación de acceso** (`ValidateAccessAndGetSectionAsync`): Valida usuario, perfil, módulo y rol. Busca la sección con `IsActive AND DeletedAt == null` y verifica que pertenezca al almacén indicado.
   - Errores: `ERP:SECTION_NOT_FOUND`, `ERP:SECTION_WAREHOUSE_MISMATCH`, `ERP:003`, `ERP:02`, `ERP:03`, `ERP:004`, `ERP:005`, `ERP:006`.

2. **Validación del `targetType`**: Debe ser `1` (LotsPositions) o `2` (RackPositions). Si es `0` (None) → `Validation_Error`.

3. **Procesamiento según `targetType`**:
   
   **Para LotsPositions (`targetType = 1`):**
   - Itera cada elemento en `lotsPositionsInformation`.
   - Busca la `LotsPositions` por `LotPositionId` con `DeletedAt == null`.
   - Si no existe → **Log Warning** y **continúa con la siguiente** (no falla la request completa).
   - Verifica que la posición pertenezca a la `sectionId` de la ruta.
   - Verifica que no tenga coordenadas ya registradas en `lots_positions_coordinates`.
   - Crea entidad `LotsPositionsCoordinates` y la registra vía `_unitOfWork.LotsPositionsCoordinates.RegisterLotPositionCoordinate()`.
   
   **Para RackPositions (`targetType = 2`):**
   - Itera cada elemento en `rackPositionsInformation`.
   - Busca la `RackPositions` por `RackPositionId` con `DeletedAt == null`.
   - Si no existe → **Log Warning** y **continúa con la siguiente**.
   - Verifica que la posición pertenezca a la `sectionId` de la ruta.
   - Verifica que no tenga coordenadas ya registradas en `rack_positions_coordinates`.
   - Crea entidad `RacksPositionsCoordinates` y la registra vía `_unitOfWork.RacksPositionsCoordinates.RegisterRackPositionCoordinate()`.

4. **Persistencia**: Si se registró al menos una coordenada, ejecuta `await _unitOfWork.SaveChangesAsync(cancellationToken)` en una sola transacción.

5. **Respuesta**: Retorna `200 OK` sin cuerpo. En los logs se informa cuántas coordenadas se registraron.

> **Comportamiento tolerante a fallos parciales**: Si una posición no existe, ya tiene coordenadas, o no pertenece a la sección, se **omite silenciosamente** (con log Warning) y se procesa la siguiente. La request no falla por posiciones individuales inválidas.

> **Contexto de la request**: `company_id`, `module_code`, `warehouse_id`, `section_id` llegan por la ruta; `user_id` se lee de `HttpContext.Items["UserId"]`; el body contiene `targetType` y los arrays de coordenadas.

---

## Validación (FluentValidation) - `RegisterCoordinatesValidator`

| Regla | Descripción |
|-------|-------------|
| `WarehouseId` | Requerido, distinto de `Guid.Empty`. |
| `SectionId` | Requerido, distinto de `Guid.Empty`. |
| `TargetType` | No puede ser `None` (0). Debe ser `LotsPositions` (1) o `RackPositions` (2). |
| `LotsPositionsInformation` | No vacío cuando `TargetType == LotsPositions`. |
| `RackPositionsInformation` | No vacío cuando `TargetType == RackPositions`. |
| Cada `LotPositionId` | Requerido, distinto de `Guid.Empty`. |
| Cada `RackPositionId` | Requerido, distinto de `Guid.Empty`. |
| Coordenadas (`PositionX`, `PositionY`, `PositionZ`) | Requeridas, precisión máx. 6 decimales (numeric(18,6)). |
| `RotationY` | Requerida, entre 0 y 360, precisión máx. 6 decimales. |

---

## Respuestas

### ✅ 200 OK

Registro exitoso (aunque sea parcial). El controller retorna `OkResult`, por lo que la respuesta es `200` sin cuerpo.

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:SECTION_NOT_FOUND",
    "description": "La sección indicada no existe o no está activa."
  },
  "createdAt": "2026-10-06 10:30:00"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | `La sección no pertenece al almacén indicado.` |
| `Validation_Error` | Errores del `RegisterCoordinatesValidator`: `El id del almacén es requerido.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `Debe especificar el tipo de coordenadas a registrar...`, `Debe proporcionar al menos una posición...`, `El id de la posición del tramo es requerido.`, `La coordenada X es requerida.`, `La rotación Y debe estar entre 0 y 360`, etc. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | `El rol asignado no es válido.` |

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
  "CreatedAt": "2026-10-06 10:30:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|--------|-------------|
| `200` | Coordenadas procesadas (registradas las válidas, omitidas las inválidas). |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Endpoints Relacionados

| Método | Endpoint | Uso |
|--------|----------|-----|
| `GET` | `.../sections/{section_id}/lots` | Lista tramos de la sección (incluye posiciones). |
| `GET` | `.../sections/{section_id}/racks` | Lista racks de la sección (incluye posiciones). |
| `POST` | `.../sections/{section_id}/lots` | Crea tramos y sus posiciones. |
| `POST` | `.../sections/{section_id}/racks` | Crea racks y sus posiciones. |