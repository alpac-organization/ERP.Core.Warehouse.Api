# Catálogo de Equipos

## Listar Equipos

Endpoint para obtener la lista paginada de equipos.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/equipment` |
| **Descripción** | Retorna los equipos paginados con filtros opcionales. Requiere autenticación con token. |
| **Tags** | `Catálogo de Equipos` |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Parámetros de Consulta (Query Params)

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `type` | `enum (EquipmentType)` | No | - | Filtrar por tipo de equipo. |
| `branch_id` | `guid` | No | - | Filtrar por sucursal (solo `Administrator`). |
| `page_number` | `int` | No | `1` | Número de página. |
| `page_size` | `int` | No | `10` | Tamaño de página. |

> **Restricción por rol:** `Administrator` puede ver equipos de todas las sucursales (filtro opcional por `branch_id`); otros roles solo ven los equipos de su sucursal.

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |
| `X-Api-Key` | `{api_key}` | Sí |