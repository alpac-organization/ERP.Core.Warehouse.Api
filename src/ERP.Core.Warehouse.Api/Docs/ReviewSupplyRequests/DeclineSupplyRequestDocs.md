# Revisión de Solicitudes

## Rechazar Solicitud (Revisión)

Endpoint unificado para rechazar solicitudes. Soporta dos tipos de solicitud según el código enviado.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/supply-requests/{supply_request_id}/decline` |
| **Descripción** | Rechaza la solicitud, cambia su estado a `Annulled` y registra el usuario que la anuló (`AnnulledByUserChiefId`) para auditoría. Requiere autenticación con token. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `supply_request_id` | `guid` | Sí | Identificador único de la solicitud a rechazar. |

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
| `code` | `string` | Sí | Código de la solicitud a rechazar. Si inicia por `FR`, se interpreta como solicitud de combustible; en otro caso, como solicitud de insumos. |
| `observations` | `string` | No | Observaciones opcionales. Máximo 1000 caracteres. |

### Ejemplo: Solicitud de insumos

```json
{
  "code": "SI-00001",
  "observations": "Rechazada por falta de presupuesto"
}
```

### Ejemplo: Solicitud de combustible

```json
{
  "code": "FR-00001",
  "observations": "Rechazada por falta de disponibilidad"
}
```

---

## Escenario 1: Solicitud de insumos

Se utiliza cuando `code` no inicia por `FR`.

- El `supply_request_id` debe corresponder a una `SupplyRequest`.
- Se valida que `code` coincida con la solicitud.
- El estado de la solicitud pasa a `Annulled`.
- Se registra `AnnulledByUserChiefId`.

## Escenario 2: Solicitud de combustible

Se utiliza cuando `code` inicia por `FR`.

- El `supply_request_id` debe corresponder a una `FuelRequest`.
- Se valida que `code` coincida con la solicitud.
- El estado de la solicitud pasa a `Annulled`.
- Se registra `AnnulledByUserChiefId`.

---

## Respuestas

### ✅ 204 No Content
Solicitud rechazada correctamente.

### ❌ 400 Bad Request
Error de validación o reglas de negocio.

### 🔍 404 Not Found
La solicitud no existe.

### 💥 500 Internal Server Error
Error no controlado del servidor.

---

## Cambios recientes

- Al rechazar, se revierte la reserva de stock (Supply y Fuel).
- Se recalcula `ProductStock.Status` (`Empty`/`LowBalance`/`Stable`) según el stock disponible tras la reversión.