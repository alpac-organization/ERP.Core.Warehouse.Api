# Salidas de Inventario

## Rechazar Despacho

Endpoint para rechazar una salida de inventario que se encuentra en estado `OutputPending`. Cambia el estado a `Rejected` y notifica por correo a los usuarios relacionados con la solicitud asociada. Requiere autenticación con token.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/inventory-outputs/{inventory_output_id}/reject-dispatch` |
| **Descripción** | Rechaza una salida de inventario pendiente, registrando la justificación y notificando a los involucrados. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `inventory_output_id` | `guid` | Sí | Identificador único de la salida de inventario. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |
| `Content-Type` | `application/json` | Sí |

---

## Body

### Ejemplo

```json
{
  "dispatched_justification": "Producto dañado, no apto para despacho"
}
```

**Campos del body (`RejectInventoryOutputCommand`):**

| Campo | Tipo | Requerido | Default | Descripción |
|-------|------|-----------|---------|-------------|
| `dispatched_justification` | `string \| null` | No | `"Rechazado"` | Justificación del rechazo. Si se envía vacío o nulo, se registra `"Rechazado"`. |

---

## Respuestas

### 204 No Content

Se rechazó la salida correctamente. Sin cuerpo de respuesta.

### 400 Bad Request

Error de validación o acceso.

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-11-06 08:00:00"
}
```

Errores específicos del handler:

| `type_error` | `description` |
|--------------|---------------|
| `ERP:INVENTORY_OUTPUT_NOT_PENDING` | `Solo se puede rechazar una salida pendiente.` |
| `ERP:INVALID_ACCESS` | `No tiene acceso para realizar esta acción.` |

### 404 Not Found

La salida de inventario no existe.

### 500 Internal Server Error

Error no controlado del servidor.

---

## Notas

| Regla | Descripción |
|-------|-------------|
| Estado requerido | La salida debe estar en `OutputPending`. |
| Efecto | `OutputPending → Rejected`. |
| Notificación | Se envía un correo a los usuarios solicitantes y aprobadores (jefe) de la solicitud asociada (supply o fuel), si tienen email configurado. |
| Auditoría | Se registra `DispatchedJustification`, `DispatchedAt` y `DispatchedByUserId`. |
| Roles permitidos | `Administrator` y `Supervisor`. |
