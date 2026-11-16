# Solicitudes de Combustible

## Registrar Solicitud de Combustible

Endpoint para registrar una nueva solicitud de combustible.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/inventory/fuel-requests` |
| **Descripción** | Registra una solicitud de combustible para una maquinaria o vehículo, validando stock disponible, maquinaria y sucursal. Requiere autenticación con token. |

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
| `Content-Type` | `application/json` | Sí |

---

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|:---------:|-------------|
| `justification` | `string` | Sí | Justificación de la solicitud de combustible. |
| `machinery_id` | `guid` | Sí | Identificador de la maquinaria o vehículo. |
| `product_stock_id` | `guid` | Sí | Identificador del stock de combustible. |
| `requested_quantity` | `decimal` | Sí | Cantidad de combustible solicitada. |
| `mileage` | `decimal` | No | Kilometraje del vehículo (opcional). |
| `engine_hours` | `decimal` | No | Horas motor de la maquinaria (opcional). |
| `customer_id` | `guid` | No* | Cliente (requerido solo cuando la sucursal es Corinto). |

### Ejemplo de Request

```json
{
  "justification": "Combustible para operación de maquinaria",
  "machinery_id": "3fa85f64-5717-4562-b3fc-2c963f66afb2",
  "product_stock_id": "3fa85f64-5717-4562-b3fc-2c963f66afb3",
  "requested_quantity": 25,
  "mileage": 12345.5,
  "engine_hours": 850
}
```

---

## Respuestas

### 201 Created

Solicitud de combustible registrada correctamente. Retorna `CreatedResult` sin cuerpo.

### 400 Bad Request

Error de validación o reglas de negocio.

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "La maquinaria no existe."
  },
  "created_at": "2026-11-05 08:00:00"
}
```

### 500 Internal Server Error

Error no controlado del servidor.

---

## Cambios recientes

- Validación por centro de costo del solicitante: el stock debe pertenecer al mismo `CostCenterId` del usuario.
- Al reservar stock se actualiza `ProductStock.Status` (`Empty`/`LowBalance`/`Stable`) según el mínimo.
- Se generan cuerpos de notificación de stock bajo (email/push) sin envío hasta existir lista de destinatarios.