# Revisión de Solicitudes

## Aprobar Solicitud (Revisión)

Endpoint unificado para aprobar solicitudes. Soporta dos tipos de solicitud según el código enviado.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/supply-requests/{supply_request_id}/approve` |
| **Descripción** | Aprueba la solicitud, cambia su estado a `Approved`, genera la salida de inventario asociada y registra el usuario aprobador (`ApprovedByUserChiefId`) para auditoría. Requiere autenticación con token. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `supply_request_id` | `guid` | Sí | Identificador único de la solicitud a aprobar. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|:---------:|
| `Authorization` | `Bearer {token}` | Sí |
| `Content-Type` | `application/json` | Sí |

---

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|:---------:|-------------|
| `code` | `string` | Sí | Código de la solicitud. Se usa para determinar el tipo de solicitud. Si inicia por `FR`, se interpreta como solicitud de combustible; en otro caso, como solicitud de insumos. |
| `reason_inventory_removal_id` | `guid` | Sí | Motivo de salida de inventario. |
| `observations` | `string` | No | Observaciones opcionales. Máximo 1000 caracteres. |

### Ejemplo: Solicitud de insumos

```json
{
  "code": "SI-00001",
  "reason_inventory_removal_id": "3fa85f64-5717-4562-b3fc-2c963f66afb4",
  "observations": "Aprobada para mantenimiento"
}
```

### Ejemplo: Solicitud de combustible

```json
{
  "code": "FR-00001",
  "reason_inventory_removal_id": "3fa85f64-5717-4562-b3fc-2c963f66afb4",
  "observations": "Aprobada para combustible"
}
```

---

## Escenario 1: Solicitud de insumos

Se utiliza cuando `code` no inicia por `FR`.

- El `supply_request_id` debe corresponder a una `SupplyRequest`.
- Se valida que `code` coincida con la solicitud.
- El estado de la solicitud pasa a `Approved`.
- Se genera y registra `InventoryOutput` con `supply_request_id`.
- Se registra `ApprovedByUserChiefId`.

## Escenario 2: Solicitud de combustible

Se utiliza cuando `code` inicia por `FR`.

- El `supply_request_id` debe corresponder a una `FuelRequest`.
- Se valida que `code` coincida con la solicitud.
- El estado de la solicitud pasa a `Approved`.
- Se genera y registra `InventoryOutput` con `fuel_request_id`.
- Se registra `ApprovedByUserChiefId`.

---

## Respuestas

### ✅ 204 No Content
Solicitud aprobada correctamente.

### ❌ 400 Bad Request
Error de validación o reglas de negocio.

### 🔍 404 Not Found
La solicitud no existe.

### 💥 500 Internal Server Error
Error no controlado del servidor.