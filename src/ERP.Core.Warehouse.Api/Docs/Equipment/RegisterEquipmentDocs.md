# Catálogo de Equipos

## Registrar Equipo

Endpoint para registrar un nuevo equipo en el catálogo, en la sucursal indicada.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/equipment` |
| **Descripción** | Registra un equipo en la compañía/sucursal indicada y la persiste con `Status = Available`. Requiere rol `Administrator`. |
| **Tags** | `Catálogo de Equipos` |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |
| `X-Api-Key` | `{api_key}` | Sí |

---

## Request Body

El JSON usa **`SnakeCaseLower`** y es **case-insensitive**.

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|:---------:|-------------|
| `branch_id` | `guid` | Sí | Sucursal a la que pertenece el equipo (debe pertenecer a la misma compañía). |
| `brand` | `string` | Sí | Marca del equipo (máx. 100). |
| `code` | `string` | Sí | Código del equipo (máx. 50, único). |
| `year` | `string` | Sí | Año del equipo (4 dígitos, ej. `2023`). |
| `model` | `string` | Sí | Modelo (máx. 100). |
| `serial_number` | `string` | Sí | Número de serie (máx. 100, único). |
| `color` | `string` | No | Color (máx. 100). |
| `type` | `enum (EquipmentType)` | Sí | Tipo de equipo (viaja como string). |
| `gasoline` | `bool` | Sí* | Bandera de combustible gasolina. |
| `diesel` | `bool` | Sí* | Bandera de combustible diésel. |
| `electric` | `bool` | Sí* | Bandera de combustible eléctrico. |
| `lpg` | `bool` | Sí* | Bandera de combustible GLP. |

> * Al menos una de las banderas de combustible debe ser `true`.

### Ejemplo

```json
{
  "branch_id": "11111111-2222-3333-4444-555555555555",
  "brand": "Toyota",
  "code": "EQ-0001",
  "year": "2023",
  "model": "7FDU35",
  "serial_number": "7FDU-2023-000123",
  "color": "Naranja/Negro",
  "type": "Forklift",
  "gasoline": false,
  "diesel": true,
  "electric": false,
  "lpg": false
}
```

---

## Respuestas

### ✅ 200 OK
Equipo registrado exitosamente.

### ❌ 400 Bad Request
- `ERP:INVALID_ACCESS`: rol no es `Administrator`.
- `ERP:BRANCH_NOT_FOUND` / `ERP:BRANCH_COMPANY_MISMATCH`: sucursal inválida o de otra compañía.
- `ERP:EQUIPMENT_CODE_DUPLICATED` / `ERP:EQUIPMENT_SERIAL_DUPLICATED`: código o serie duplicado.
- Errores de validación de campos.