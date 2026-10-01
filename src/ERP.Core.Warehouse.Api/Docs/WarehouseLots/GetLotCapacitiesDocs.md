# Tramos

## Obtener Capacidades de Tramo

Endpoint para consultar las capacidades de un tramo específico dentro de una sección de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{sections_id}/lots/{lot_id}/capacities` |
| **Descripción** | Retorna las dimensiones y las áreas de capacidad del tramo indicado. |
| **Tags**        | `Tramos` |

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

## Flujo del Handler (`GetLotCapacitiesHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Busca la sección con `Id == sections_id AND IsActive AND DeletedAt == null`. Si no existe: `ERP:SECTION_NOT_FOUND`.
3. Verifica que `section.WarehouseId == warehouse_id`. Si no: `ERP:SECTION_WAREHOUSE_MISMATCH`.
4. Busca el tramo con `Id == lot_id AND SectionId == sections_id AND DeletedAt == null`, con `Include(LotsCapacity)`. Si no existe: `ERP:LOT_NOT_FOUND`.
5. Si el tramo existe pero no tiene fila en `lots_capacity`: `ERP:LOT_CAPACITY_NOT_FOUND`.
6. Mapea `LotsCapacity` a `LotCapacitiesDto` con `LotsProfile` y lo devuelve.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y `warehouse_id`, `sections_id` y `lot_id` los asigna el action.

> **Validación (FluentValidation):** el `GetLotCapacitiesValidator` valida `warehouse_id` requerido, `sections_id` requerido y distinto de `Guid.Empty`, y `lot_id` requerido y distinto de `Guid.Empty`.

> A diferencia del listado de tramos, este handler **sí** usa `Include(LotsCapacity)` porque el mapeo ocurre en memoria después de materializar la entidad. Sin ese `Include`, `lot.LotsCapacity` llegaría `null` y siempre respondería `ERP:LOT_CAPACITY_NOT_FOUND`.

---

## Respuestas

### ✅ 200 OK

Devuelve un `LotCapacitiesDto`. El JSON se serializa con `SnakeCaseLower`.

```json
{
  "lots_id": "56487c1b-9f4d-4b2a-8e1c-1234567890ab",
  "width": 10.00,
  "length": 8.00,
  "total_area_m2": 80.00,
  "available_area_with_margin_m2": 75.00,
  "unused_area_m2": 5.00,
  "unoccupied_chargeable_area_m2": 70.00,
  "occupied_chargeable_area_m2": 10.00,
  "percentage_available_area_with_margin_m2": 87.50
}
```

| Campo                                     | Tipo      | Descripción |
|-------------------------------------------|-----------|-------------|
| `lots_id`                                 | `guid`    | Id del tramo. |
| `width`                                   | `decimal` | Ancho del tramo en metros. |
| `length`                                  | `decimal` | Largo del tramo en metros. |
| `total_area_m2`                           | `decimal` | Área total del tramo (ancho × largo). |
| `available_area_with_margin_m2`           | `decimal` | Área disponible descontando el margen de maniobra. |
| `unused_area_m2`                          | `decimal` | Área no cargable. |
| `unoccupied_chargeable_area_m2`           | `decimal` | Área cargable libre de occupation. |
| `occupied_chargeable_area_m2`             | `decimal` | Área cargable ya ocupada. |
| `percentage_available_area_with_margin_m2` | `decimal` | Porcentaje de área disponible con margen. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Fuente de datos | Lee `lots_capacity`, no el tramo. Los valores reflejan el último recálculo. |
| Áreas | Las calcula `ILotCapacityCalculator`. Este endpoint no recalcula: solo devuelve lo persistido. |
| Sin sección en la respuesta | Los identificadores viajan en la ruta, no en el body. |
| Tramo dado de baja | Un tramo con `DeletedAt` informado responde `ERP:LOT_NOT_FOUND`. |

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
| `ERP:LOT_CAPACITY_NOT_FOUND` | `No se encontró la capacidad del tramo.` |
| `ERP:SECTION_NOT_FOUND` | `La sección indicada no existe o no está activa.` |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | `La sección no pertenece al almacén indicado.` |
| `Validation_Error` | Errores del `GetLotCapacitiesValidator`: `El id del almacén es requerido.`, `El id de la sección es requerido.`, `El id de la sección no es válido.`, `El id del tramo es requerido.`, `El id del tramo no es válido.` |
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
| `200` | Capacidades obtenidas exitosamente. |
| `400` | Error de validación, acceso o si el tramo no existe (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |