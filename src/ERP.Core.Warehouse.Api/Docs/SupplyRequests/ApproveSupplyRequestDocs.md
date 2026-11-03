# Solicitudes Operativas de Inventario

## Aprobar Solicitud de Insumos (Revisión)

Endpoint para aprobar una solicitud de insumos en la etapa de revisión por supervision/administración.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/supply-requests/{supply_request_id}/approve` |
| **Descripción** | Aprueba la solicitud, cambia su estado a `Approved` y genera/registrar la salida de inventario asociada. Requiere autenticación con token. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `supply_request_id` | `guid` | Sí | Identificador único de la solicitud de insumos a aprobar. |

## Headers

| Header | Valor | Requerido |
|--------|-------|:---------:|
| `Authorization` | `Bearer {token}` | Sí |
| `Content-Type` | `application/json` | Sí |

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|:---------:|-------------|
| `reason_inventory_removal_id` | `guid` | Sí | Motivo de salida de inventario. |
| `observations` | `string` | No | Observaciones opcionales. Máximo 1000 caracteres. |

```json
{
  "reason_inventory_removal_id": "3fa85f64-5717-4562-b3fc-2c963f66afb4",
  "observations": "Aprobada para mantenimiento"
}
```

## Respuestas

### ✅ 204 No Content
Solicitud aprobada correctamente.

### ❌ 400 Bad Request
Error de validación o reglas de negocio.

### 🔍 404 Not Found
La solicitud de insumos no existe.

### 💥 500 Internal Server Error
Error no controlado del servidor.
