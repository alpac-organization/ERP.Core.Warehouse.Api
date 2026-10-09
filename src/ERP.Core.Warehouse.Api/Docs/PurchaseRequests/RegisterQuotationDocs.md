# Cotizaciones

## Registrar Cotización

Endpoint para registrar una o más cotizaciones sobre ítems de una solicitud de compra ya aprobada. Calcula precio unitario (preferencial o base), subtotal e IVA; permite adjuntar imágenes/PDF y crear el vínculo producto–proveedor si no existe.

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/quotations` |
| **Descripción** | Crea cotizaciones por ítem × supplier. El backend resuelve precio desde `SupplierProductTierPrice` o `SupplierProduct.UnitPrice`, calcula IVA si el producto no es exento, sube adjuntos a S3 y opcionalmente crea la relación N:M producto–proveedor. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `quotation_items` | `array` | Sí | Lista de cotizaciones. Al menos una. |

### Elemento `quotation_items[]`

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `supplier_id` | `guid` | Sí | Proveedor que cotiza. |
| `purchase_request_item_id` | `guid` | Sí | Ítem de la solicitud a cotizar. |
| `has_delivery` | `boolean` | Sí | Si incluye entrega. |
| `has_guarantee` | `boolean` | Sí | Si incluye garantía. |
| `inventory_available` | `boolean` | No | Default `true`. Inventario disponible. |
| `price_unit` | `decimal \| null` | Condicional | Override opcional. **Obligatorio** si el vínculo producto–proveedor no tiene precio. |
| `supplier_selection_justification` | `string` | Sí | Justificación de selección del proveedor. |
| `brand_product` | `string` | No | Marca. Máx. 200. |
| `product_quality` | `integer` | Sí | Calidad del producto (`ProductQuality`). |
| `payment_method_type` | `integer` | Sí | Método de pago (`PaymentMethodType`). |
| `delivery_time` | `decimal` | Condicional | Obligatorio si `has_delivery = true`. |
| `delivery_time_type` | `integer` | Condicional | Obligatorio si `has_delivery = true`. |
| `warranty_period` | `decimal` | Condicional | Obligatorio si `has_guarantee = true`. |
| `warranty_period_time_type` | `integer` | Condicional | Obligatorio si `has_guarantee = true`. |
| `availability_time` | `decimal` | No | Tiempo de disponibilidad. |
| `availability_time_type` | `integer` | No | Tipo de tiempo de disponibilidad. |
| `create_supplier_product_if_missing` | `boolean` | No | Default `true`. Crea el vínculo producto–proveedor si no existe. |
| `images` | `array` | No | Imágenes opcionales (base64). |
| `documents` | `array` | No | PDFs opcionales (base64). |

### Elemento `images[]` / `documents[]`

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `file_name` | `string` | Condicional | Obligatorio en documentos PDF. En imágenes es opcional. |
| `base64_content` | `string` | Sí | Contenido base64 (con o sin prefijo `data:*;base64,`). |

---

## Ejemplo

```json
{
  "quotation_items": [
    {
      "supplier_id": "44444444-4444-4444-4444-444444444444",
      "purchase_request_item_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
      "has_delivery": true,
      "has_guarantee": true,
      "inventory_available": true,
      "price_unit": null,
      "supplier_selection_justification": "Mejor tiempo de entrega",
      "brand_product": "HP",
      "product_quality": 1,
      "payment_method_type": 2,
      "delivery_time": 3,
      "delivery_time_type": 2,
      "warranty_period": 12,
      "warranty_period_time_type": 3,
      "create_supplier_product_if_missing": true,
      "images": [
        {
          "file_name": "evidencia.jpg",
          "base64_content": "data:image/jpeg;base64,/9j/4AAQ..."
        }
      ],
      "documents": [
        {
          "file_name": "cotizacion.pdf",
          "base64_content": "JVBERi0xLjcK..."
        }
      ]
    }
  ]
}
```

---

## Cálculos del backend (no enviar)

El frontend **no** debe enviar `iva` ni `price_total`; el API los calcula:

| Campo persistido | Fórmula / origen |
|---|---|
| `price_unit` | 1) Tier preferencial vigente si `quantity >= min_quantity` y fechas válidas; 2) `SupplierProduct.UnitPrice`; 3) `price_unit` del request |
| `price_total` | `quantity × price_unit` (subtotal sin IVA) |
| `iva` | Si producto **no** exento: `price_total × (tasa_IVA / 100)` desde `ValidityDeductions` (`TaxType.Iva = 6`). Si exento: `0` |
| `supplier_product_id` | Id del vínculo usado/creado |
| `additional_data` | JSON con `images[]` y `documents[]` (URLs S3) |

### Precio preferencial (Tier)

Se aplica si existe un `SupplierProductTierPrice` activo donde:

1. `quantity` del ítem ≥ `min_quantity`
2. `valid_from` ≤ fecha de cotización
3. `valid_to` es `null` o ≥ fecha de cotización

Si hay varios tiers, se toma el de mayor `min_quantity`.

---

## Respuestas

### 201 Created

Cotización(es) registrada(s). Cuerpo vacío.

### 400 Bad Request

| Situación | Código |
|---|---|
| Rol Supervisor | `ERP:INVALID_ACCESS` |
| Vínculo inexistente y `create_supplier_product_if_missing = false` | `ERP:SUPPLIER_PRODUCT_NOT_FOUND` |
| Sin precio en catálogo y sin `price_unit` | `ERP:PRICE_UNIT_REQUIRED` |
| Validación FluentValidation | 400 genérico |

### 404 Not Found

| Situación | Código |
|---|---|
| Ítem de solicitud no existe | `ERP:PURCHASE_REQUEST_ITEM_NOT_FOUND` |
| Supplier inexistente/inactivo | `ERP:SUPPLIER_NOT_FOUND` |
| Producto del ítem no existe | `ERP:PRODUCT_NOT_FOUND` |

---

## Reglas de negocio

| Regla | Comportamiento |
|---|---|
| Rol `Supervisor` | No puede cotizar. |
| `has_delivery = true` | Exige `delivery_time` y `delivery_time_type`. |
| `has_guarantee = true` | Exige `warranty_period` y `warranty_period_time_type`. |
| Adjuntos | Imágenes → S3 `Compras/Cotizaciones` vía `UploadImageAsync`; PDFs → `UploadPdfAsync`. |
| Ítem sin cotización previa | Marca `HasQuotation = true` en el ítem. |
| Relación nueva | Crea `SupplierProduct` con `unit_price` = `price_unit` enviado o `0`. |

---

## Códigos de estado

| Código | Descripción |
|:---:|---|
| `201` | Creado exitosamente. |
| `400` | Validación o regla de negocio. |
| `404` | Recurso no encontrado. |
| `500` | Error interno. |

---

## Enums

| Enum | Valores |
|---|---|
| `PaymentMethodType` | `1` ACH, `2` LocalTransfer, `3` Check, `4` Cash, `5` InternationalWire |
| `ProductQuality` | Según catálogo Core |
| `TimeType` | Según catálogo Core (días/semanas/meses, etc.) |
