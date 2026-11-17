# Catálogo de Equipos

## Actualizar Equipo (PATCH)

Endpoint para actualizar parcialmente un equipo.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/equipment/{equipment_id}` |
| **Descripción** | Actualiza parcialmente un equipo. Requiere rol `Administrator`. |
| **Tags** | `Catálogo de Equipos` |

---

## Request Body (parcial)

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|:---------:|-------------|
| `branch_id` | `guid` | No | Nueva sucursal (debe pertenecer a la misma compañía). |
| `brand` | `string` | No | Marca (máx. 100). |
| `code` | `string` | No | Código (máx. 50, único). |
| `year` | `string` | No | Año (4 dígitos). |
| `model` | `string` | No | Modelo (máx. 100). |
| `serial_number` | `string` | No | Número de serie (máx. 100, único). |
| `color` | `string` | No | Color (máx. 100). |
| `status` | `enum (EquipmentStatus)` | No | Estado. |
| `type` | `enum (EquipmentType)` | No | Tipo. |
| `is_active` | `bool` | No | Activo/inactivo. |
| `gasoline` / `diesel` / `electric` / `lpg` | `bool` | No | Banderas de combustible (si se envía alguna, al menos una debe quedar `true`). |

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
  "status": "Available",
  "type": "Forklift",
  "is_active": true,
  "gasoline": false,
  "diesel": true,
  "electric": false,
  "lpg": false
}
```

---

## Respuestas

### ✅ 200 OK
Equipo actualizado exitosamente.

### ❌ 400 Bad Request
- `ERP:INVALID_ACCESS`: rol no es `Administrator`.
- `ERP:BRANCH_COMPANY_MISMATCH`: sucursal no pertenece a la compañía.
- `ERP:EQUIPMENT_CODE_DUPLICATED` / `ERP:EQUIPMENT_SERIAL_DUPLICATED`: código o serie duplicado.
- `ERP:EQUIPMENT_FUEL_FLAG_REQUIRED`: ninguna bandera de combustible activa.