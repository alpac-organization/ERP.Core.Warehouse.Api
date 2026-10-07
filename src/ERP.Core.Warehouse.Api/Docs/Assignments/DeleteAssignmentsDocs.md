# Asignaciones Operativas

## Eliminar Asignación Operativa

Endpoint para eliminar una asignación operativa existente.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `DELETE` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}` |
| **Descripción** | Elimina la asignación operativa especificada. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `operational_order_id` | `guid` | Sí | Identificador único de la orden operativa. |
| `assignment_id` | `guid` | Sí | Identificador único de la asignación a eliminar. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 204 No Content

Asignación eliminada correctamente. Sin cuerpo de respuesta.

### 400 Bad Request

Error de validación, asignación no encontrada, no se puede eliminar por tener dependencias, sin permiso, etc. (`ErrorResponse`).

### 500 Internal Server Error

Error no controlado del servidor.