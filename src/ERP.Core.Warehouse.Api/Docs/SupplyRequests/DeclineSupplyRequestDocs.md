# Solicitudes Operativas de Inventario

## Rechazar Solicitud de Insumos (Revisión)

Endpoint para rechazar una solicitud de insumos en la etapa de revisión por supervision/administración.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/supply-requests/{supply_request_id}/decline` |
| **Descripción** | Rechaza la solicitud y cambia su estado a `Annuled`. Requiere autenticación con token. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `supply_request_id` | `guid` | Sí | Identificador único de la solicitud de insumos a rechazar. |

## Headers

| Header | Valor | Requerido |
|--------|-------|:---------:|
| `Authorization` | `Bearer {token}` | Sí |
| `Content-Type` | `application/json` | Sí |

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|:---------:|-------------|
| `observations` | `string` | No | Observaciones opcionales. Máximo 1000 caracteres. |

```json
{
  "observations": "Rechazada por falta de presupuesto"
}
```

## Respuestas

### ✅ 204 No Content
Solicitud rechazada correctamente.

### ❌ 400 Bad Request
Error de validación o reglas de negocio.

### 🔍 404 Not Found
La solicitud de insumos no existe.

### 💥 500 Internal Server Error
Error no controlado del servidor.
