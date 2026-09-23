# Aduanas (Custom Branches)

## Obtener Ramas de Aduanas

Endpoint para obtener la lista paginada de ramas de aduanas activas dentro del módulo de aduanas.

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/customs-branches` |
| **Descripción** | Retorna la lista paginada de ramas de aduanas activas, ordenadas por fecha de creación descendente (más recientes primero). |

---

## Parámetros de Ruta 

| Parámetro     | Tipo     | Requerido | Descripción |
|:-------------:|:--------:|-----------|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo dentro de la compañía. |

---

## Parámetros de Consulta (Query Parameters)

| Parámetro      | Tipo      | Requerido | Valor por defecto | Descripción |
|:--------------:|:---------:|-----------|-------------------|-------------|
| `page_size`    | `integer` | No        | `10`              | Número de registros por página. |
| `page_number`  | `integer` | No        | `1`               | Número de página a consultar. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `X-Api-Key`     | `{api_key}`        | Sí        |

---

## Respuestas

### ✅ 200 OK

Retorna la lista paginada de ramas de aduanas activas.

**Estructura de respuesta (`PagedResponse<CustomsBranchDto>`):**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `data` | `array<CustomsBranchDto>` | Lista de ramas de aduanas. |
| `pageNumber` | `integer` | Número de página actual. |
| `pageSize` | `integer` | Tamaño de página solicitado. |
| `total` | `integer` | Total de registros encontrados. |

#### CustomsBranchDto

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `customBranchId` | `guid` | Identificador único de la rama de aduana. |
| `code` | `string \| null` | Código de la rama de aduana (ej: `ADU-001`). |
| `customsBranchName` | `string \| null` | Nombre de la rama de aduana (ej: `Aduana Principal Guayaquil`). |

---

### Ejemplo de Respuesta (200 OK)

```json
{
  "data": [
    {
      "custom_branch_id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "code": "ADU-001",
      "customs_branch_name": "Aduana Principal Guayaquil"
    },
    {
      "custom_branch_id": "b2c3d4e5-f6a7-8901-bcde-f23456789012",
      "code": "ADU-002",
      "customs_branch_name": "Aduana Quito"
    }
  ],
  "page_number": 1,
  "page_size": 10,
  "total": 2
}
```

---

### ❌ 400 Bad Request

Error de validación, acceso o reglas de negocio usando `ErrorResponse`:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-23 08:00:00"
}
```

#### Códigos de error específicos de acceso:

| Código | Descripción |
|--------|-------------|
| `ERP:003` | Este usuario no existe. |
| `ERP:02` | Usuario se encuentra temporalmente bloqueado, comunicar con el área de informática. |
| `ERP:03` | Usuario se encuentra inactivo, comunicar con el área de informática. |
| `ERP:004` | No existe un perfil asociado a esta empresa. |
| `ERP:005` | No tienes acceso a este módulo. |
| `ERP:006` | El rol asignado no es válido. |

---

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud"
  },
  "created_at": "2026-09-23 08:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Lista de ramas de aduanas obtenida exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |

---

## Reglas de Negocio

1. **Solo ramas activas**: El endpoint filtra automáticamente solo las ramas de aduanas donde `IsActive = true`.
2. **Ordenamiento**: Los resultados se ordenan por `CreatedAt` descendente (más recientes primero).
3. **Paginación**: Se aplica paginación usando `Skip()` y `Take()` basado en `page_number` y `page_size`.
4. **Validación de acceso**: Se valida que el usuario exista, esté activo, tenga perfil en la compañía y acceso al módulo solicitado.
5. **Rendimiento**: La consulta usa `AsNoTracking()` para optimizar consultas de solo lectura.

---

## Validaciones de Entrada

| Campo | Reglas |
|-------|--------|
| `UserId` (header/context) | Requerido, GUID válido no vacío. |
| `CompanyId` (ruta) | Requerido, GUID válido no vacío. |
| `ModuleCode` (ruta) | Requerido, string no vacío. |
| `page_size` (query) | Opcional, entero positivo (default: 10). |
| `page_number` (query) | Opcional, entero positivo (default: 1). |

---

## Entidad de Base de Datos

**Tabla**: `CustomsBranches` (Catálogos)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `uniqueidentifier` | PK - Identificador único (GUID). |
| `IsActive` | `bit` | Indica si la rama está activa. |
| `Code` | `nvarchar(max)` | Código de la rama de aduana. |
| `CustomsBranchName` | `nvarchar(max)` | Nombre de la rama de aduana. |
| `CreatedAt` | `datetime` | Fecha de creación (UTC). |
| `DeletedAt` | `datetime \| null` | Fecha de eliminación suave (soft delete). |

**Entidad de dominio**: `ERP.Core.Database.Domain.Entities.Catalogs.CustomsBranches`

---

## Mapeo (AutoMapper)

```csharp
CreateMap<CustomsBranches, CustomsBranchDto>()
    .ForMember(d => d.CustomBranchId, o => o.MapFrom(s => s.Id))
    .ForMember(d => d.Code, o => o.MapFrom(s => s.Code))
    .ForMember(d => d.CustomsBranchName, o => o.MapFrom(s => s.CustomsBranchName));
```

Perfil: `ERP.Core.Warehouse.Api.Application.Commons.Mappings.CustomBranchesProfile`