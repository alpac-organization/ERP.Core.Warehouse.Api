# Tramos

## Registrar Tramos (Masivo)

Endpoint para registrar de 1 a 10 tramos de forma masiva dentro de una sección de tipo tramos (`SectionStorageType.Lots`). Todos los tramos del lote se crean con las mismas filas, columnas, ancho y largo; solo difieren en el código generado. Cada tramo genera su matriz de posiciones y su capacidad (`lots_capacity`), y actualiza en cascada las capacidades de la sección (`section_capacity`) y del almacén (`warehouse_capacity`).

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots` |
| **Descripción** | Registra hasta 10 tramos en la sección, crea sus posiciones y recalcula las capacidades en cascada. |
| **Tags**        | `Tramos` |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:-------------:|:--------:|:---------:|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid`   | Sí        | Identificador único del almacén. |
| `sections_id`  | `guid`   | Sí        | Identificador único de la sección. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `X-Api-Key`     | `{api_key}`        | Sí        |
| `Content-Type`  | `application/json` | Sí        |

---

## Request Body

`company_id`, `module_code`, `warehouse_id`, `sections_id` y `user_id` no forman parte del JSON: se toman de la ruta y del token (`[JsonIgnore]`).

El `code` de cada tramo **no se envía**: lo genera el `CodeGenerator` del core a partir del código registrado de la sección.

| Parámetro          | Tipo      | Requerido | Default | Descripción |
|--------------------|-----------|:---------:|---------|-------------|
| `quantity`         | `int`     | Sí        | —       | Cantidad de tramos a crear. Entre 1 y 10. |
| `nominal_rows`     | `int`     | Sí        | —       | Filas nominales de cada tramo. Mayor a 0. |
| `nominal_columns`  | `int`     | Sí        | —       | Columnas nominales de cada tramo. Mayor a 0. |
| `width`            | `decimal` | Sí        | —       | Ancho de cada tramo en metros. Mayor a 0, máximo 2 decimales. |
| `length`           | `decimal` | Sí        | —       | Largo de cada tramo en metros. Mayor a 0, máximo 2 decimales. |

```json
{
  "quantity": 2,
  "nominal_rows": 4,
  "nominal_columns": 5,
  "width": 10.00,
  "length": 15.00
}
```

---

## Flujo del Handler (`RegisterLotsHandler`)

1. `ValidateAccessAndGetSectionAsync` (heredado de `BaseLotsCapacityHandler`): valida acceso, busca la sección con `IsActive AND DeletedAt == null` y verifica que pertenezca al almacén. Errores: `ERP:SECTION_NOT_FOUND`, `ERP:SECTION_WAREHOUSE_MISMATCH`.
2. Si `section.SectionType == Aisle`: `ERP:SECTION_TYPE_NOT_ALLOWED_FOR_LOTS`.
3. Si `section.SectionStorageType != Lots`: `ERP:SECTION_STORAGE_MISMATCH`.
4. Cuenta los tramos no dados de baja de la sección. Si `existentes + quantity > 10`: `ERP:SECTION_LOT_LIMIT_EXCEEDED`. El mensaje incluye ambos números.
5. Pide `quantity` códigos con `ICodeGenerator.GenerateUniqueStorageCodesAsync(StorageEntityType.Lot, section.Id, quantity)`. Si la sección no tiene código registrado: `ERP:SECTION_CODE_NOT_FOUND`.
6. Por cada tramo: crea la entidad con `LotsProfile.ToLotsEntity` (id nuevo, código generado, sección, filas y columnas nominales, `Status = Available`) y la registra.
7. Por cada tramo recorre la matriz `1..NominalRows × 1..NominalColumns` y registra una `LotsPositions` con `LotId`, el código de `ICodeGenerator.GeneratePositionCode(lotCode, row, column)`, `Row`, `Column` y `Level = 1`.
8. Calcula todo el lote de una vez con `ILotCapacityCalculator.CalculateLotsAsync(section.Id, capacidades)`. Si la cantidad devuelta no coincide o la sección viene sin capacidad: `ERP:SECTION_CAPACITY_NOT_FOUND`.
9. Persiste la capacidad de cada tramo en `lots_capacity`.
10. Actualiza en cascada `section_capacity` y `warehouse_capacity` (crea la fila si no existía).
11. Guarda los cambios en una sola transacción.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y `warehouse_id` y `sections_id` los asigna el action sobre la command.

> **Validación (FluentValidation):** el `RegisterLotsValidator` valida sección y almacén obligatorios, `quantity` entre 1 y 10, filas y columnas obligatorias y mayores a 0, y `width` / `length` mayores a 0 con máximo 2 decimales.

---

## Valores por defecto de lo creado

No se envían en la petición; los aplica el handler:

| Entidad         | Campo                                          | Valor |
|-----------------|------------------------------------------------|-------|
| `lots`          | `Status`                                       | `Available` |
| `lots`          | `UnavailableReason`, `StatusChangedAt`         | `null` |
| `lots`          | `AllowsStacking`                               | No lo asigna el handler. Queda en el valor por defecto de `bool` (`false`). Para habilitarlo hay que usar el `PATCH`. |
| `lots_positions`| `Level`                                        | `1` (a piso). Los niveles de estibado se registran por otro flujo. |
| `lots_positions`| `Status`, `AllowsStocking`                      | El handler no los asigna. Quedan en el valor por defecto del tipo: `0` para el enum (que no corresponde a ningún miembro válido de `RackStatus`) y `false`. |

> **Atención:** los `Status` de las posiciones quedan en `0`, que no es un valor válido de `RackStatus` (`Available = 1` a `Reserved = 5`). Conviene confirmarlo contra los datos reales antes de exponer el listado de posiciones.

---

## Posiciones generadas

Por cada tramo se crea una matriz `nominal_rows × nominal_columns` de posiciones. Con `nominal_rows = 4` y `nominal_columns = 5` son 20 posiciones por tramo; con `quantity = 2` son 40 en total.

| Regla | Valor |
|-------|-------|
| `PositionCode` | Lo produce `ICodeGenerator.GeneratePositionCode(lot.Code, row, column)`. El formato exacto está definido en el core. |
| `Row`, `Column` | Índices de la matriz, ambos en base 1. |
| `Level` | Siempre `1`. |

---

## Respuestas

### ✅ 201 Created

Los tramos fueron creados correctamente. La respuesta es `201` sin cuerpo.

### Notas

| Campo / regla | Descripción |
|---|---|
| Límite por petición | `quantity` entre 1 y 10. |
| Límite por sección | Máximo 10 tramos no dados de baja por sección. El chequeo es `existentes + quantity > 10`, no `quantity > 10`: si ya hay 8 tramos solo caben 2 más. |
| Tipo de sección | Debe ser `SectionStorageType.Lots`. Las secciones tipo pasillo y las de otro tipo de almacenamiento se rechazan. |
| Código generado | Secuencial y único dentro de la sección, derivado del código registrado de la sección. |
| Todo o nada | El lote completo se persiste en una sola transacción. Si falla el recálculo de capacidad, no queda nada creado. |
| Áreas | `width` y `length` alimentan el cálculo de áreas en m² de cada tramo. |
| Recálculo en cascada | `section_capacity` y `warehouse_capacity` se actualizan siempre, aunque el cálculo devuelva `null` para alguno de los dos, en cuyo caso se omite ese nivel. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:SECTION_LOT_LIMIT_EXCEEDED",
    "description": "La sección solo permite un máximo de 10 tramos (8 existentes + 3 solicitados)."
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:SECTION_LOT_LIMIT_EXCEEDED` | `La sección solo permite un máximo de 10 tramos (N existentes + M solicitados).` |
| `ERP:SECTION_TYPE_NOT_ALLOWED_FOR_LOTS` | `No se pueden crear tramos en una sección de tipo pasillo.` |
| `ERP:SECTION_STORAGE_MISMATCH` | `Esta sección no admite tramos.` |
| `ERP:SECTION_CODE_NOT_FOUND` | `La sección no tiene un código registrado para generar el código de los tramos.` |
| `ERP:SECTION_CAPACITY_NOT_FOUND` | `La sección no tiene capacidad registrada para recalcular.` |
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | `La sección no pertenece al almacén indicado.` |
| `Validation_Error` | Errores del `RegisterLotsValidator`: `La sección es obligatoria.`, `El almacén es obligatorio.`, `La cantidad de tramos debe ser mayor que 0.`, `Se permite un máximo de 10 tramos por petición.`, `Las filas son obligatorias.`, `Las filas deben ser mayor que 0.`, `Las columnas son obligatorias.`, `Las columnas deben ser mayor que 0.`, `El ancho (metros) debe ser mayor que 0.`, `El ancho admite máximo 2 decimales.`, `El largo (metros) debe ser mayor que 0.`, `El largo admite máximo 2 decimales.` |
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
| `201` | Tramos registrados exitosamente. |
| `400` | Error de validación, acceso o reglas de negocio (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `SectionStorageType`

| Valor | Nombre    | Descripción |
|:-----:|-----------|-------------|
| `1`   | `Racks`   | Sección de racks. No admite tramos. |
| `2`   | `Lots`    | Sección de tramos. **Único tipo que acepta este endpoint.** |
| `3`   | `Pallets` | Sección de pallets. |
| `4`   | `None`    | Sin tipo de almacenamiento. |

### `SectionType`

| Valor | Nombre    | Descripción |
|:-----:|-----------|-------------|
| `1`   | `Storage` | Sección de almacenamiento. |
| `2`   | `Aisle`   | Pasillo. Rechazado por este endpoint. |

### `RackStatus`

Valor inicial de cada tramo creado: `Available`.

| Valor | Nombre            | Descripción |
|:-----:|-------------------|-------------|
| `1`   | `Available`       | Listo y libre para asignar mercadería. |
| `2`   | `Occupied`        | Tiene mercadería asignada actualmente. |
| `3`   | `UnderMaintenance`| Fuera de servicio por mantenimiento. |
| `4`   | `Blocked`         | Inhabilitado por otra causa. |
| `5`   | `Reserved`        | Apartado para una operación en curso. |