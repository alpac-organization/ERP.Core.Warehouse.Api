# Asignaciones Operativas

## Listar Maquinaria Asignada

Endpoint para consultar la maquinaria asignada a una asignación operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/machinery` |
| **Descripción** | Lista la maquinaria **activa** y no dada de baja de la asignación operativa, con el tipo, código y marca de cada maquinaria y el usuario que la asignó. |
| **Tags**        | `Asignaciones operacionales` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo dentro de la compañía. |
| `operational_order_id` | `guid`   | Sí        | Identificador de la orden operativa que contiene la asignación. |
| `assignment_id`        | `guid`   | Sí        | Identificador de la asignación operativa. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `x-api-key`     | `{api_key}`      | Sí        |

---

## Parámetros de Query

| Parámetro     | Tipo  | Requerido | Default | Descripción |
|---------------|-------|:---------:|---------|-------------|
| `page_number` | `int` | No        | `1`     | Número de página. Debe ser mayor a cero. |
| `page_size`   | `int` | No        | `10`    | Cantidad de registros por página. Debe ser mayor a cero. |

---

## Flujo del Handler (`GetAssignmentMachineryHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla, responde `400`.
2. Busca la asignación operativa con `Include(OperationalOrder)`. Si no existe: `ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND`.
3. Verifica que `OperationalOrderId` coincida con el de la ruta: `ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH`.
4. Verifica que la orden operativa pertenezca a la compañía: `ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH` (`403`).
5. Consulta `assignments_machinery` filtrando por `AssignmentOperationalId`, `IsActive = true` y `DeletedAt IS NULL`, con `Include(Machinery)` e `Include(User)`, `AsNoTracking`.
6. Cuenta el total, pagina con `Skip`/`Take` y ordena por `CreatedAt` ascendente.
7. Mapea con `AssignmentProfile` a `GetAssignmentMachineryDto` y devuelve el `PagedResponse`.

> **Validación (FluentValidation):** el `GetAssignmentMachineryValidator` (`BaseRequestValidator`) valida `operational_order_id`, `assignment_id`, `page_size > 0` y `page_number > 0`.

> Los `Include` de `Machinery` y `User` son obligatorios: el mapeo ocurre en memoria después de materializar la entidad, así que sin ellos las rutas navegadas llegan `null` y el perfil de AutoMapper falla al construirse.

---

## Respuestas

### ✅ 200 OK

Devuelve un `PagedResponse<GetAssignmentMachineryDto>`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`).

```json
{
  "data": [
    {
      "assignment_machinery_id": "7c4e1a92-0000-0000-0000-000000000001",
      "machinery_id": "3f2a1b4c-0000-0000-0000-000000000001",
      "concept": "Carga inicial de bodega",
      "is_active": true,
      "created_by_user_name": "jperez",
      "assignment_operational_id": "5e8b2c14-0000-0000-0000-000000000001",
      "machinery_information": {
        "machinery_type": "Forklift",
        "machinery_code": "MAQ-0001",
        "machinery_brand": "Toyota"
      }
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

| Campo                        | Tipo                     | Descripción |
|------------------------------|--------------------------|-------------|
| `assignment_machinery_id`    | `guid`                   | Id de la fila de asignación. **No** es el id de la maquinaria. Es el valor que consume el DELETE. |
| `machinery_id`               | `guid`                   | Id de la maquinaria (`AssignmentsMachinery.MachineryId`). |
| `concept`                    | `string`                 | Concepto de la asignación. |
| `is_active`                  | `bool`                   | Siempre `true` en la respuesta, el query filtra las inactivas. |
| `created_by_user_name`       | `string`                 | `User.UserName` de quien creó la asignación. Ya no se devuelve `created_by_user_id`. |
| `assignment_operational_id`  | `guid`                   | Id de la asignación operativa. |
| `machinery_information.machinery_type`  | `enum (MachineryType)`   | Tipo de la maquinaria. Hoy el enum solo tiene `Forklift`. |
| `machinery_information.machinery_code`  | `string`                 | `Machinery.Code`. |
| `machinery_information.machinery_brand` | `string`                 | `Machinery.Brand`. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Filtro del listado | Solo `is_active = true` y `deleted_at IS NULL`. Las filas quitadas por DELETE desaparecen del listado aunque sigan en la base. |
| Orden | Por `created_at` ascendente. |
| Sin filtro de sucursal | No se filtra por compañía ni sucursal: devuelve toda la maquinaria de la asignación operativa. |
| Maquinaria repetida | Como el POST no valida duplicados, la misma maquinaria puede aparecer como varias filas con distinto `assignment_machinery_id` e igual `machinery_id`. |
| Reinicio de API | El perfil de AutoMapper se construye en runtime. Si se cambió el mapeo hay que reiniciar la API, no alcanza con recompilar. |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND",
    "description": "La asignacion operativa seleccionada no existe"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND` | `La asignacion operativa seleccionada no existe` |
| `ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH` | `La asignacion operativa no pertenece a la orden operativa indicada` |
| `Validation_Error` | Errores del `GetAssignmentMachineryValidator`: `El número de página debe ser mayor a cero.`, `El tamaño de página debe ser mayor a cero.` |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH` | `No tienes acceso a la asignacion operativa seleccionada` |
| `Forbidden` | `x-api-key` ausente o incorrecta. |

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
|---|---|
| `200` | Listado obtenido exitosamente. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `403` | Asignación operativa de otra compañía, o `x-api-key` inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |
