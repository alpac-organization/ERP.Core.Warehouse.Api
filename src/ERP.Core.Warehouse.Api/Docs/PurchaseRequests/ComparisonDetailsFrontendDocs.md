# Comparativa de cotizaciones (Finanzas / Gerencia)

Guía para construir la matriz de comparación producto × proveedores cotizados.

---

## Endpoints de lectura recomendados

| Uso | Método | Endpoint |
|---|---|---|
| Detalle de solicitud | `GET` | `/purchase-requests/{purchase_request_id}` |
| Productos paginados de la solicitud | `GET` | `/purchase-requests/{purchase_request_id}/products` |
| Detalle revisión contable | `GET` | `/requisition-accounting-reviews/{requisition_accounting_review_id}` |
| Detalle revisión gerencia | `GET` | `/requisition-management-reviews/{requisition_management_reviews_id}/details` |

Todos incluyen (cuando aplica) cotizaciones activas, datos del supplier y vínculos `supplier_products` del producto.

---

## Estructura por ítem (snake_case)

```json
{
  "purchase_request_item_id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
  "has_quotation": true,
  "quantity": 100,
  "quantity_unit": 1,
  "description": "Lapicero azul",
  "justification": "Stock bajo",
  "additional_data": "{\"images_product_to_changed\":[\"https://...\"]}",
  "product_details": {
    "product_id": "11111111-1111-1111-1111-111111111111",
    "product_name": "Lapicero azul",
    "code": "ALP-01-001",
    "is_tax_exempt": false,
    "category_information": {
      "catagory_id": "33333333-3333-3333-3333-333333333333",
      "name": "Papelería",
      "code": "PAP"
    },
    "supplier_products": [
      {
        "supplier_product_id": "sp-0001",
        "supplier_id": "44444444-4444-4444-4444-444444444444",
        "unit_price": 10.5,
        "suppliers_legal_name": "Proveedor SA",
        "commercial_name": "Proveedor"
      }
    ]
  },
  "unit_measure_information": {
    "code": "UND",
    "name": "Unidad",
    "symbol": "u"
  },
  "quotations": [
    {
      "quotation_id": "q-0001",
      "is_active": true,
      "has_delivery": true,
      "has_guarantee": true,
      "inventory_available": true,
      "is_accepted_for_purchase": false,
      "iva": 157.5,
      "price": 10.5,
      "price_unit": 10.5,
      "price_total": 1050.0,
      "quote_date": "2026-10-07",
      "brand_product": "BIC",
      "delivery_time": 3,
      "delivery_time_type": 2,
      "warranty_period": 6,
      "warranty_period_time_type": 3,
      "supplier_selection_justification": "Mejor precio",
      "product_quality": 1,
      "payment_method_type": 2,
      "additional_data": "{\"images\":[],\"documents\":[{\"file_id\":\"...\",\"file_name\":\"cotizacion.pdf\",\"file_url\":\"https://...\",\"uploaded_at\":\"2026-10-07T12:00:00Z\"}]}",
      "supplier_product_id": "sp-0001",
      "supplier_id": "44444444-4444-4444-4444-444444444444",
      "supplier_information": {
        "supplier_id": "44444444-4444-4444-4444-444444444444",
        "image_url": null,
        "suppliers_legal_name": "Proveedor SA",
        "identification_number": "J0310000000000",
        "identification_type": 1
      }
    }
  ]
}
```

---

## Campos clave para la UI de comparación

| Campo | Uso en UI |
|---|---|
| `product_details.product_name` | Fila / nombre del producto |
| `product_details.is_tax_exempt` | Badge “Exento IVA” |
| `product_details.supplier_products` | Suppliers disponibles en catálogo (aunque aún no cotizados) |
| `quotations[].price_unit` / `price_total` / `iva` | Columnas de precio |
| `quotations[].has_delivery` / `has_guarantee` / `inventory_available` | Checkmarks comparativos |
| `quotations[].delivery_time` + `delivery_time_type` | Lead time |
| `quotations[].warranty_period` + `warranty_period_time_type` | Garantía |
| `quotations[].payment_method_type` | Método de pago |
| `quotations[].additional_data` | Links a PDF/imágenes (parsear JSON) |
| `quotations[].is_accepted_for_purchase` | Cotización elegida para OC |

---

## Parseo de `additional_data` de cotización

```json
{
  "images": [
    {
      "file_id": "guid",
      "file_name": "evidencia.jpg",
      "file_url": "https://...",
      "uploaded_at": "2026-10-07T12:00:00Z"
    }
  ],
  "documents": [
    {
      "file_id": "guid",
      "file_name": "cotizacion.pdf",
      "file_url": "https://...",
      "uploaded_at": "2026-10-07T12:00:00Z"
    }
  ]
}
```

Mostrar como enlaces/previews; no reenviar base64 al actualizar salvo que se agreguen archivos nuevos.

---

## Flujo UI sugerido en Finanzas

1. Abrir detalle de revisión contable.
2. Por cada ítem, pintar tabla de cotizaciones.
3. Destacar mejor precio / mejor entrega según reglas de negocio del cliente.
4. Aceptar cotización (`accept-for-purchase`) por ítem.
5. Enviar a gerencia cuando todos los ítems necesarios tengan cotización aceptada.
