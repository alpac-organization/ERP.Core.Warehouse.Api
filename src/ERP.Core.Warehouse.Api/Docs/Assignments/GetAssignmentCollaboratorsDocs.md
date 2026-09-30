# Asignaciones Operativas

## Listar Colaboradores Asignados

Endpoint para consultar los colaboradores asignados a una asignación operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/collaborators` |
| **Descripción** | Lista los colaboradores **activos** y no dados de baja de la asignación operativa, con su nombre, área de trabajo y cargo, y el usuario que los asignó. |
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

## Flujo del Handler (`GetAssignmentCollaboratorsHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla, responde `400`.
2. Busca la asignación operativa con `Include(OperationalOrder)`. Si no existe: `ERP:ASSIGNMENT_OPERATIONAL_NOT_FOUND`.
3. Verifica que `OperationalOrderId` coincida con el de la ruta: `ERP:ASSIGNMENT_OPERATIONAL_ORDER_MISMATCH`.
4. Verifica que la orden operativa pertenezca a la compañía: `ERP:ASSIGNMENT_OPERATIONAL_COMPANY_MISMATCH` (`403`).
5. Consulta `assignment_collaborators` filtrando por `AssignmentOperationalId`, `IsActive = true` y `DeletedAt IS NULL`, con la cadena de `Include`: `Collaborator` → `WorkingInformation` → `Area` y `WorkingInformation` → `JobPosition`, además de `User`, todo `AsNoTracking`.
6. Cuenta el total, pagina con `Skip`/`Take` y ordena por `CreatedAt` ascendente.
7. Mapea con `AssignmentProfile` a `GetAssignmentCollaboratorsDto` y devuelve el `PagedResponse`.

> **Validación (FluentValidation):** el `GetAssignmentCollaboratorsValidator` (`BaseRequestValidator`) valida `operational_order_id`, `assignment_id`, `page_size > 0` y `page_number > 0`.

> **Contexto de la request:** `company_id`, `module_code` y `operational_order_id` los inyecta `BaseAssignmentResourceController.QueryAsync` desde la ruta, y `user_id` lo lee el action de `HttpContext.Items["UserId"]` con `Guid.Parse` antes de delegar. `assignment_id`, `page_number` y `page_size` se asignan en el action.

> Este handler **no** usa `BaseAssignmentOperationalHandler`: hace las validaciones del punto 1 al 4 inline porque solo lee y no necesita entidad tracked.

> Los `Include` de la cadena son obligatorios: el mapeo ocurre en memoria después de materializar la entidad. `WorkingInformation` es una relación 1:1 y `Area` / `JobPosition` cuelgan de ella, así que sin los `ThenInclude` esas rutas llegan `null`.

> Se usan dos `Include` separados sobre `Collaborator` porque EF Core no fusiona dos ramificaciones que arrancan en la misma navegación dentro de una sola cadena.

---

## Respuestas

### ✅ 200 OK

Devuelve un `PagedResponse<GetAssignmentCollaboratorsDto>`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`).

```json
{
  "data": [
    {
      "assignment_collaborator_id": "6b2d4e91-0000-0000-0000-000000000001",
      "is_active": true,
      "collaborator_id": "8a1f3c7e-0000-0000-0000-000000000001",
      "created_by_user_name": "jperez",
      "assignment_operational_id": "5e8b2c14-0000-0000-0000-000000000001",
      "role": "WarehouseAssistant",
      "collaborator_information": {
        "collaborator_name": "Juan Carlos Pérez López",
        "work_area_name": "Bodega Central",
        "job_position_name": "Auxiliar de bodega"
      }
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

| Campo                                          | Tipo                              | Descripción |
|------------------------------------------------|-----------------------------------|-------------|
| `assignment_collaborator_id`                   | `guid`                            | Id de la fila de asignación. **No** es el id del colaborador. Es el valor que consume el DELETE. |
| `is_active`                                    | `bool`                            | Siempre `true` en la respuesta, el query filtra las inactivas. |
| `collaborator_id`                              | `guid`                            | Id del colaborador. |
| `created_by_user_name`                         | `string`                          | `User.UserName` de quien creó la asignación. |
| `assignment_operational_id`                    | `guid`                            | Id de la asignación operativa. |
| `role`                                         | `enum (AssignmentCollaboratorsRoles)` | Rol de la asignación. |
| `collaborator_information.collaborator_name`   | `string`                          | `FirstName`, `SecondName`, `FirstLastname`, `SecondLastname` unidos con espacio, descartando los vacíos. |
| `collaborator_information.work_area_name`      | `string`                          | `Collaborator.WorkingInformation.Area.WorkAreaName`. |
| `collaborator_information.job_position_name`   | `string`                          | `Collaborator.WorkingInformation.JobPosition.JobPositionName`. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Filtro del listado | Solo `is_active = true` y `deleted_at IS NULL`. Las filas quitadas por DELETE desaparecen del listado aunque sigan en la base. |
| Orden | Por `created_at` ascendente. |
| Sin filtro de sucursal | El listado no filtra por sucursal: devuelve todos los colaboradores de la asignación operativa. |
| Rol vs. cargo | `role` es el rol de la asignación (`WarehouseAssistant` / `ForkliftOperator`) y `job_position_name` es el cargo real del colaborador. Son datos distintos y ambos vienen en la respuesta. |
| Valores `null` | `work_area_name` y `job_position_name` pueden llegar `null` si el colaborador no tiene `WorkingInformation`, o si su `Area` o `JobPosition` es nula en la base. Las propiedades están declaradas como no-nullables, pero AutoMapper no rellena nada cuando se corta la cadena de navegación. |
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
| `Validation_Error` | Errores del `GetAssignmentCollaboratorsValidator`: `El número de página debe ser mayor a cero.`, `El tamaño de página debe ser mayor a cero.` |
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
