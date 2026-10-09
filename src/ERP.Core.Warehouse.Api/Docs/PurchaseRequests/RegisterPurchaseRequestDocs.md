# Solicitudes de compras

## Registrar Solicitud de Compra

Endpoint para registrar una o más solicitudes de compra. Cada ítem puede referenciar un **producto existente** o crear un **producto nuevo** en catálogo con vínculos a proveedores (relación N:M).

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/purchase-requests` |
| **Descripción** | Crea solicitudes de compra con ítems. Si el ítem trae `new_product`, se inserta el producto en catálogo, se genera código único y se crean vínculos `SupplierProduct` con los proveedores indicados. Las imágenes en base64 se suben a S3. |

---

## Parámetros de Ruta

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

`user_id`, `company_id` y `module_code` no van en el JSON: se toman del token y de la ruta.

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `purchase_requests` | `array` | Sí | Lista de solicitudes a crear. Al menos una. |

### Elemento `purchase_requests[]`

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `branch_id` | `guid` | Sí | Sucursal solicitante. |
| `cost_center_id` | `guid` | Sí | Centro de costo. |
| `area_id` | `guid` | No | Solo Admin puede forzar área; Operator/Manager usan la de su perfil. |
| `observations` | `string` | No | Concepto / observaciones. Máx. 1000 caracteres. |
| `priority_level` | `integer` | Condicional | Obligatorio (≠ `None`) si `request_type` es Requisición. |
| `destination` | `integer` | Sí | Destino de la solicitud (`DestinationRequest`). |
| `request_type` | `integer` | Sí | Tipo de solicitud (`PurchaseRequestType`). |
| `purchase_request_items` | `array` | Sí | Ítems solicitados. Al menos uno. |

### Elemento `purchase_request_items[]`

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `product_id` | `guid \| null` | Condicional | Producto existente. Obligatorio si no se envía `new_product`. |
| `new_product` | `object \| null` | Condicional | Datos para crear producto nuevo. Obligatorio si no hay `product_id`. |
| `unit_measure_id` | `guid` | Condicional | Obligatorio cuando se usa producto existente. |
| `quantity` | `integer` | Sí | Cantidad solicitada. Debe ser > 0. |
| `quantity_unit` | `integer` | No | Cantidad por unidad. Si se envía, > 0. |
| `description` | `string` | No | Descripción del ítem. |
| `justification` | `string` | No | Justificación de la compra. |
| `additional_data` | `string (JSON)` | No | Data adicional. Para imágenes: `{"images_product_to_changed":["base64..."]}`. |

### Objeto `new_product`

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `product_name` | `string` | Sí | Nombre del producto. Máx. 200. |
| `description` | `string` | No | Descripción del producto. |
| `category_id` | `guid` | Sí | Categoría del catálogo. |
| `unit_measure_id` | `guid` | Sí | Unidad de medida del producto. |
| `product_usage_type` | `integer` | Sí | `1` Insumo, `2` OperationalUse. |
| `is_tax_exempt` | `boolean` | Sí | Si `true`, no se calcula IVA en cotizaciones. |
| `suppliers` | `array` | No | Proveedores a vincular al crear el producto. |

### Elemento `new_product.suppliers[]`

| Parámetro | Tipo | Requerido | Descripción |
|---|---|:---:|---|
| `supplier_id` | `guid` | Sí | Proveedor a relacionar. |
| `unit_price` | `decimal \| null` | No | Precio unitario inicial del vínculo. Si se omite, queda en `0`. |

---

## Ejemplos

### Producto existente

```json
{
  "purchase_requests": [
    {
      "branch_id": "00000000-0000-0000-0000-000000000001",
      "cost_center_id": "00000000-0000-0000-0000-000000000002",
      "observations": "Compra de insumos",
      "priority_level": 3,
      "destination": 1,
      "request_type": 1,
      "purchase_request_items": [
        {
          "product_id": "11111111-1111-1111-1111-111111111111",
          "unit_measure_id": "22222222-2222-2222-2222-222222222222",
          "quantity": 100,
          "quantity_unit": 1,
          "justification": "Stock bajo",
          "description": "Lapicero azul",
          "additional_data": "{\"images_product_to_changed\":[\"iVBORw0KGgo...\"]}"
        }
      ]
    }
  ]
}
```

### Producto nuevo + suppliers

```json
{
  "purchase_requests": [
    {
      "branch_id": "00000000-0000-0000-0000-000000000001",
      "cost_center_id": "00000000-0000-0000-0000-000000000002",
      "priority_level": 3,
      "destination": 1,
      "request_type": 1,
      "purchase_request_items": [
        {
          "product_id": null,
          "unit_measure_id": "22222222-2222-2222-2222-222222222222",
          "quantity": 50,
          "justification": "Producto nuevo requerido",
          "new_product": {
            "product_name": "Toner HP 85A",
            "description": "Compatible",
            "category_id": "33333333-3333-3333-3333-333333333333",
            "unit_measure_id": "22222222-2222-2222-2222-222222222222",
            "product_usage_type": 1,
            "is_tax_exempt": false,
            "suppliers": [
              {
                "supplier_id": "44444444-4444-4444-4444-444444444444",
                "unit_price": 350.00
              },
              {
                "supplier_id": "55555555-5555-5555-5555-555555555555",
                "unit_price": null
              }
            ]
          }
        }
      ]
    }
  ]
}
```

---

## Respuestas

### 201 Created

Solicitud(es) registrada(s) correctamente. Cuerpo vacío.

### 400 Bad Request

Validación o reglas de negocio (`ErrorResponse`).

### 403 / 404

Sin permiso (rol `Supervisor`) o recursos no encontrados (producto, categoría, unidad, proveedor).

---

## Reglas de negocio

| Regla | Comportamiento | Código |
|---|---|---|
| Rol `Supervisor` | No puede registrar. | `ERP:INVALID_ACCESS` |
| Producto o new_product | Debe enviarse uno de los dos. | Validación FluentValidation |
| Producto inexistente | Si se envía `product_id` inválido. | `ERP:PRODUCT_NOT_FOUND` |
| Categoría / UOM / Supplier | Deben existir al crear producto nuevo. | `ERP:CATEGORY_NOT_FOUND` / `ERP:UNIT_MEASURE_NOT_FOUND` / `ERP:SUPPLIER_NOT_FOUND` |
| Código de producto | Se genera con `GenerateUniqueProductCode(companyId, categoryId)`. | `ERP:ERROR_PRODUCT_CODE_GENERATOR` |
| Imágenes | Base64 → S3 (`Compras` / `SolicitudesCompras`); se guardan URLs. | — |
| Notificación | Se notifica a Manager/Admin del módulo. | — |

---

## Códigos de estado

| Código | Descripción |
|:---:|---|
| `201` | Creado exitosamente. |
| `400` | Validación o regla de negocio. |
| `403` | Sin permiso. |
| `404` | Recurso no encontrado. |
| `500` | Error interno. |
