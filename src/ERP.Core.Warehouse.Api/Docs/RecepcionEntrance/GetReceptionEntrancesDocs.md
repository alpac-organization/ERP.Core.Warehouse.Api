# Recepciones

## Listar Recepciones

Endpoint para listar con paginación las recepciones registradas, con filtros opcionales por tipo de documento, placa del vehículo y número de contenedor.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` |
| **Descripción** | Lista las recepciones ordenadas por fecha de creación (descendente). |
| **Tags**        | `Control de Acceso` |

---

## Parámetros de Ruta (Path Params)

| Parámetro     | Tipo     | Requerido | Descripción |
|:------------:|:--------:|:---------:|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo (ej. `WAREHOUSE`). |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |
| `X-Api-Key`     | `{api_key}`      | Sí        |

---

## Parámetros de Query

| Parámetro              | Tipo                  | Requerido | Default | Descripción |
|------------------------|-----------------------|:---------:|---------|-------------|
| `only_day`             | `bool`                | No        | `true`  | **No se aplica.** El parámetro se recibe y se asigna al query, pero el handler nunca lo usa. |
| `plate_number`         | `string`              | No        | `null`  | Coincidencia parcial (`Contains`) sobre `ReceptionTransport.VehiclePlateNumber`. |
| `contaniner_number`    | `string`              | No        | `null`  | Coincidencia **exacta** sobre `ContainerNumber`. |
| `document_number`      | `string`              | No        | `null`  | **No se aplica.** El parámetro se recibe y se asigna al query, pero el handler nunca lo usa como filtro. |
| `document_type`        | `enum (DocumentType)` | No        | `null`  | Filtra si **alguna** orden operacional de la recepción tiene ese tipo de documento. |
| `page_number`          | `int`                 | No        | `1`     | Número de página. |
| `page_size`            | `int`                 | No        | `10`    | Registros por página. |

> ⚠️ **El nombre del parámetro de contenedor está mal escrito en el código fuente.** Se llama `contaniner_number` (con la "i" antes de la "n"), no `container_number`. Hay que enviarlo exactamente así.

---

## Flujo del Handler (`GetReceptionEntrancesHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario, perfil de compañía y acceso al módulo. Si falla devuelve `400`.
2. Arma el query base: `ReceptionEntrance WHERE IsActive` con `Include(ReceptionTransport)`, `Include(OperationalOrders)` y `AsNoTracking()`.
3. Si viene `document_type`, filtra por `OperationalOrders.Any(po => po.DocumentType == document_type)`.
4. Si viene `plate_number`, filtra por `ReceptionTransport.VehiclePlateNumber.Contains(plate_number)`.
5. Si viene `contaniner_number`, filtra por `ContainerNumber == contaniner_number` (igualdad exacta).
6. Cuenta el total, pagina con `Skip`/`Take` y ordena por `CreatedAt` descendente.
7. Mapea con `ReceptionEntranceProfile` a `ReceptionEntranceDto` y devuelve el `PagedResponse`.

> **Contexto de la request:** `company_id` y `module_code` llegan por la ruta, `user_id` se lee de `HttpContext.Items["UserId"]`, y el resto de parámetros los asigna el action.

> **Validación (FluentValidation):** el `GetReceptionEntrancesValidator` está **vacío**. Solo se ejecutan las reglas de `BaseRequestValidator` (`company_id`, `module_code`, `user_id`). No se valida `page_number`, `page_size` ni ningún filtro de búsqueda, incluido `document_type`, que no se valida contra el enum.

> **El listado no filtra por `DeletedAt`**, solo por `IsActive == true`. Las recepciones dadas de baja por `DELETE` desaparecen del listado en tanto su `IsActive` se marque como `false`; mientras tanto siguen apareciendo.

> **`document_type` se resuelve a través de las órdenes operacionales.** La recepción no guarda el tipo de documento en una columna propia: el DTO lo deriva de `OperationalOrders.FirstOrDefault().DocumentType` en el mapper. Si la recepción no tiene ninguna orden operacional, el valor es `default`, es decir, el primer miembro declarado del enum.

### ⚠️ Parámetros muertos

`only_day` y `document_number` se exponen en el contrato, viajan en el query y **no producen ningún efecto en el resultado**. En particular:

- `only_day=true` (el default) **no** limita el listado al día actual: se devuelven recepciones de cualquier fecha.
- `document_number` **no** filtra por número de DUCA ni de declaración aduanera, aunque el nombre lo sugiera.

Enviar cualquiera de los dos con un valor distinto del default no cambia nada. No los uses para construir filtros en el cliente esperando que el servidor los aplique.

### ⚠️ Trampas de paginación

| Situación | Resultado |
|---|---|
| `page_size` negativo | Lanza excepción no controlada al evaluar `Take(page_size)`, produciendo `500 Server_Error`. El validator no valida la paginación. |
| `page_number = 0` | `Skip((0 - 1) * page_size)` produce un `Skip` negativo, que PostgreSQL interpreta como `0`. Devuelve la primera página. |
| `page_number` mayor que el total | `200` con `data: []` y el `total` real. No es un error. |

---

## Respuestas

### ✅ 200 OK

Devuelve un `PagedResponse<ReceptionEntranceDto>`. El JSON se serializa con `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`). Las horas viajan como `HH:mm:ss`.

```json
{
  "data": [
    {
      "reception_code": "REC-00045",
      "reception_entrance_id": "9a1f2c3d-4e5b-6a7f-8c9d-0e1f2a3b4c5d",
      "vehicle_plate_number": "MGA-1234",
      "vehicle_exit_time": null,
      "container_exit_time": null,
      "seal_number": "SEAL-8899",
      "container_number": "CONT-4455",
      "country_of_origin": "Panamá",
      "document_type": "DUCA"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 1
}
```

| Campo                   | Tipo                  | Descripción |
|-------------------------|-----------------------|-------------|
| `reception_code`        | `string`              | Código generado de la recepción. |
| `reception_entrance_id` | `guid`                | Id de la recepción. Es el valor que consumen el detalle y el PATCH. |
| `vehicle_plate_number`  | `string`              | Placa del vehículo, tomada de la información de transporte. |
| `vehicle_exit_time`     | `time`                | Hora de salida del vehículo. `null` si no ha salido. |
| `container_exit_time`   | `time`                | Hora de salida del contenedor. `null` si no ha salido. |
| `seal_number`           | `string`              | Número de sello. |
| `container_number`      | `string`              | Número de contenedor. |
| `country_of_origin`     | `string`              | País de origen. |
| `document_type`         | `enum (DocumentType)` | Tipo de documento, derivado de la primera orden operacional de la recepción. |

### Notas

| Campo / regla | Descripción |
|---|---|
| Orden | Por `created_at` descendente: las más recientes primero. |
| Filtro de `plate_number` | Parcial, sensible a mayúsculas según la collation de la base de datos. No se aplica `Trim()` ni se normaliza a minúsculas. |
| Filtro de contenedor | Exacto, **no parcial**. `CONT-4455` no coincide con `CONT-44551`. |
| Filtro de `document_type` | Semántica `Any`: si la recepción tiene varias órdenes operacionales y una sola coincide, la recepción aparece. |
| `vehicle_exit_time` / `container_exit_time` | Siempre `null` en este endpoint: el handler de registro no los escribe y ningún endpoint de la API los actualiza todavía. El propio controller tiene el comentario `//Endpoint para darle continuidad al registro vehicular y salid de reception.` pendiente de implementar. |
| `document_type` | No es una columna de la recepción. Viene de `OperationalOrders.FirstOrDefault()`. Sin órdenes, es el valor `default` del enum. |
| Detalle | Este listado no trae `additional_data`, ni la información de transporte completa, ni la aduana. Para eso está el endpoint de detalle. |

### ❌ 400 Bad Request

Respuesta del `ExceptionMiddleware` con `CoreException` (camelCase):

```json
{
  "status": 400,
  "error": {
    "typeError": "ERP:005",
    "description": "No tienes acceso a este módulo"
  },
  "createdAt": "2026-09-30 14:32:10"
}
```

| `typeError` | `description` |
|-------------|---------------|
| `Validation_Error` | Errores de `BaseRequestValidator`: `company_id`, `module_code` y `user_id` requeridos. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

> **Este endpoint no restringe por rol.** No hay comprobación de `Supervisor` ni de ningún otro `RoleType`; a diferencia del registro y la actualización, el listado de recepciones es accesible para todos los roles con acceso al módulo.

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
| `200` | Listado obtenido exitosamente. |
| `400` | Error de validación o acceso (`ErrorResponse` camelCase). |
| `403` | `X-Api-Key` ausente o inválida. |
| `500` | Error interno del servidor (`Server_Error` PascalCase). |

---

## Catálogos de Enums Utilizados

### `DocumentType`

> ⚠️ **Confirmar valores numéricos.** El enum vive en el paquete NuGet `ERP.Core.Database.Domain` y sus valores enteros no son legibles desde este repositorio. La documentación anterior de este módulo manejaba cifras distintas entre el registro y la actualización, por lo que **aquí no se documentan números**: no hay forma de verificarlos sin descompilar el paquete.

Los miembros referenciados en el código del módulo son:

| Miembro             | Uso |
|---------------------|-----|
| `DUCA`              | Recepción con uno o varios números de DUCA. Genera una orden operacional por cada número. |
| `CustomsDeclaration`| Recepción con número de declaración aduanera. Genera una sola orden operacional. |

---

## Endpoints Relacionados

| Endpoint | Descripción |
|----------|-------------|
| `GET /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}/details` | Detalle completo de una recepción. De ahí se obtiene el `reception_entrance_id`. |
| `POST /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` | Registra una recepción y genera sus órdenes operacionales. |
| `PATCH /api/v1/companies/{company_id}/modules/{module_code}/reception-entrances/{reception_entrance_id}` | Actualiza los datos de una recepción. Ventana de 10 minutos. |