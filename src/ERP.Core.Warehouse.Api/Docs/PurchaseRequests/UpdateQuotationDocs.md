# Cotizaciones

## Actualizar Cotización

Endpoint para actualizar de forma parcial una cotización existente. Recalcula subtotal e IVA cuando cambia el precio o hay tier aplicable. Permite agregar imágenes y PDFs adicionales.

| Campo | Valor |
|---|---|
| **Método** | `PATCH` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/quotations/{quotation_id}` |
| **Descripción** | Actualización parcial. Solo se modifican los campos enviados. Si se envían `images` o `documents`, se **agregan** a `additional_data` (no reemplazan los existentes). |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo. |
| `quotation_id` | `guid` | Sí | Cotización a actualizar. Debe existir y estar activa. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

Todas las propiedades son **opcionales** (PATCH parcial).

| Parámetro | Tipo | Descripción |
|---|---|---|
| `supplier_id` | `guid` | Nuevo proveedor. |
| `has_delivery` | `boolean` | Incluye entrega. |
| `has_guarantee` | `boolean` | Incluye garantía. |
| `inventory_available` | `boolean` | Inventario disponible. |
| `price_unit` | `decimal` | Precio unitario. Si se envía, se recalcula `price_total` e `iva`. |
| `brand_product` | `string` | Marca. |
| `product_quality` | `integer` | Calidad. |
| `payment_method_type` | `integer` | Método de pago. |
| `delivery_time` | `decimal` | Tiempo de entrega. |
| `delivery_time_type` | `integer` | Tipo de tiempo de entrega. |
| `warranty_period` | `decimal` | Período de garantía. |
| `warranty_period_time_type` | `integer` | Tipo de período de garantía. |
| `availability_time` | `decimal` | Tiempo de disponibilidad. |
| `availability_time_type` | `integer` | Tipo de tiempo de disponibilidad. |
| `supplier_selection_justification` | `string` | Justificación. |
| `images` | `array` | Imágenes a agregar (`file_name`, `base64_content`). |
| `documents` | `array` | PDFs a agregar (`file_name`, `base64_content`). |

### Ejemplo

```json
{
  "has_delivery": true,
  "delivery_time": 5,
  "delivery_time_type": 2,
  "price_unit": 12.50,
  "payment_method_type": 3,
  "documents": [
    {
      "file_name": "cotizacion-actualizada.pdf",
      "base64_content": "JVBERi0xLjcK..."
    }
  ]
}
```

---

## Respuestas

### 200 OK

Cotización actualizada. Cuerpo vacío.

### 400 Bad Request

Rol `Supervisor` u otras reglas → `ERP:INVALID_ACCESS`.

### 404 Not Found

Cotización inexistente o inactiva → `ERP:QUOTATION_NOT_FOUND`.

---

## Reglas de negocio

| Regla | Comportamiento |
|---|---|
| Rol `Supervisor` | No puede actualizar. |
| Recálculo de totales | Si hay `price_unit` o tier aplicable: `price_total = quantity × price_unit`; `iva` según exención del producto. |
| Adjuntos | Se **fusionan** con los ya guardados en `additional_data`. |
| Cotización inactiva | No se actualiza (404). |

---

## Códigos de estado

| Código | Descripción |
|:---:|---|
| `200` | Actualizado exitosamente. |
| `400` | Validación o sin permiso. |
| `404` | Cotización no encontrada. |
| `500` | Error interno. |
