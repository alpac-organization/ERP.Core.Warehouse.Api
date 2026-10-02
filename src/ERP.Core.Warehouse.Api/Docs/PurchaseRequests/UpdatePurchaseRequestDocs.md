# Solicitudes de compras

## Actualizar Solicitud de Compra

Endpoint para actualizar de forma parcial una solicitud de compra y/o sus ítems dentro del módulo de purchase.

| Campo | Valor |
|-------|-------|
| **Método**      | `PATCH` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}` |
| **Descripción** | Actualiza parcialmente la cabecera de una solicitud de compra (`observations`, `priority_level`, `destination_request`) y/o sus ítems: con `id` actualiza un ítem existente; sin `id` crea un ítem nuevo (con subida de imágenes a S3 si aplica). Solo aplica si la solicitud está activa y en estado `Pending`. |

---

## Parámetros de Ruta 

| Parámetro             | Tipo     | Requerido | Descripción |
|:---------------------:|:--------:|-----------|-------------|
| `company_id`          | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`         | `string` | Sí        | Código del módulo dentro de la compañía. |
| `purchase_request_id` | `guid`   | Sí        | Identificador de la solicitud de compra a actualizar. Debe existir y estar activa. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code` y `purchase_request_id` no forman parte del JSON: se toman del token y de la ruta (`[JsonIgnore]` / route).

Todas las propiedades del body son **opcionales** (PATCH parcial). Solo se actualiza lo que se envía con valor.

| Parámetro                | Tipo                                      | Requerido | Descripción |
|--------------------------|-------------------------------------------|-----------|-------------|
| `observations`           | `string`                                  | No        | Observaciones / concepto de la solicitud. Máximo 1000 caracteres. Se mapea a `Concept`. |
| `priority_level`         | `integer (enum PriorityLevel)`            | No        | Nivel de prioridad. Se envía como **número**. Si el tipo de solicitud es Requisición, no puede ser `None`. En otros tipos solo puede ser `None`. |
| `destination_request`    | `integer (enum DestinationRequest)`       | No        | Destino de la solicitud. Se envía como **número**. |
| `purchase_request_items` | `array`                                   | No        | Lista de ítems a actualizar y/o crear. Si se omite o viene vacía, solo se actualiza la cabecera. |

### Ítems (`purchase_request_items[]`)

| Parámetro                   | Tipo            | Requerido | Descripción |
|-----------------------------|-----------------|-----------|-------------|
| `id`                        | `guid`          | Condicional | Con `id`: actualiza el ítem existente. Sin `id` / `null`: **crea** un ítem nuevo. |
| `quantity`                  | `integer`       | Condicional | En alta (sin `id`) es **obligatorio** y debe ser mayor a cero. En update es opcional; si se envía, debe ser mayor a cero. |
| `quantity_unit`             | `integer`       | No        | Cantidad por unidad. Si se envía, debe ser mayor a cero. |
| `product_id`                | `guid`          | Condicional | En alta es **obligatorio**. En update es opcional. No puede ser `Guid.Empty`. |
| `unit_measure_id`           | `guid`          | Condicional | En alta es **obligatorio**. En update es opcional. No puede ser `Guid.Empty`. |
| `description`               | `string`        | No        | Descripción del ítem. |
| `justification`             | `string`        | No        | Justificación del ítem. |
| `images_product_to_changed` | `array<string>` | No        | En **alta**: imágenes en Base64; se suben a S3 (`Compras` / `SolicitudesCompras`) y las URLs se guardan en `AdditionalData`. En **update** de ítem existente: se guardan tal cual en `AdditionalData` (sin subir a S3). |

#### Ejemplo: actualizar ítem existente

```json
{
  "observations": "Actualización de la solicitud",
  "priority_level": 1,
  "destination_request": 1,
  "purchase_request_items": [
    {
      "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "quantity": 10,
      "quantity_unit": 2,
      "product_id": "11111111-1111-1111-1111-111111111111",
      "unit_measure_id": "22222222-2222-2222-2222-222222222222",
      "description": "Producto editado",
      "justification": "Justificación actualizada"
    }
  ]
}
```

#### Ejemplo: crear ítem nuevo

```json
{
  "purchase_request_items": [
    {
      "quantity": 5,
      "quantity_unit": 1,
      "product_id": "11111111-1111-1111-1111-111111111111",
      "unit_measure_id": "22222222-2222-2222-2222-222222222222",
      "description": "Producto nuevo",
      "justification": "Se requiere adicional",
      "images_product_to_changed": [
        "data:image/png;base64,iVBORw0KGgo..."
      ]
    }
  ]
}
```

---

## Respuestas

### ✅ 200 OK

La solicitud se actualizó correctamente. El cuerpo de la respuesta puede ir vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| PATCH parcial | Solo se sobrescriben propiedades enviadas con valor (`HasValue` / `!= null`). |
| Estado | Solo se actualizan solicitudes con `RequestStatus = Pending`. |
| Activa | La solicitud debe existir y tener `IsActive = true`. |
| Ítems | Con `id` se actualiza; sin `id` se crea. No hay delete. |
| Alta de ítem | Requiere `product_id`, `unit_measure_id` y `quantity`. |
| Imágenes en alta | Base64 → S3 → URLs en `AdditionalData`. |
| `purchase_request_items` vacío / ausente | Solo se actualiza la cabecera (si vino algún campo). |
| Rol `Supervisor` | Recibe 400: `No tienes permiso para realizar esta acción`. |
| Prioridad inválida | Recibe 400: reglas de prioridad según el tipo de solicitud (`ERP:INVALID_PRIORITY`). |
| Solicitud no pendiente | Recibe 400: `Solo se pueden actualizar solicitudes en estado pendiente.` |
| Solicitud no encontrada | Recibe error desde el handler cuando no existe o está inactiva. |
| Ítem no encontrado | Recibe error: `Uno de los productos de la solicitud no existe.` |
| Enums (`priority_level`, `destination_request`) | Se envían como **enteros**. FluentValidation valida con `IsInEnum` cuando vienen con valor. |

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`) en errores de validación FluentValidation, acceso y reglas de negocio:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-10 08:00:00"
}
```

Si un enum se envía con tipo incorrecto (por ejemplo string), la respuesta puede venir en el formato de model binding de ASP.NET (no `ErrorResponse`), por ejemplo:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "$.priority_level": [
      "The JSON value could not be converted to ERP.Core.Database.Domain.Enums.PriorityLevel."
    ]
  }
}
```

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud"
  },
  "created_at": "2026-09-10 08:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `200` | Solicitud de compra actualizada exitosamente. |
| `400` | Error de validación, acceso, permisos, reglas de negocio (`ErrorResponse`) o fallo de deserialización del body (model binding). |
| `500` | Error interno del servidor (`ErrorResponse`). |
