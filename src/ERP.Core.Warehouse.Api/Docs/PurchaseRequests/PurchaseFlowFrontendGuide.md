# Guía frontend — Flujo de compras (N:M Producto–Proveedor)

Documento de referencia para consumir el flujo de compras actualizado: relación muchos a muchos entre productos y proveedores, creación inline de productos, cotizaciones con precios/IVA/adjuntos y retenciones IMI/IR al emitir órdenes de compra.

---

## Convención general

| Aspecto | Valor |
|---|---|
| JSON | Propiedades en **snake_case** |
| Enums | Se envían/reciben como **número** |
| Auth | Header `Authorization: Bearer {token}` |
| Base | `/api/v1/companies/{company_id}/modules/{module_code}/...` |

---

## Flujo completo

```text
1. Registrar solicitud (producto existente o nuevo + suppliers)
        ↓
2. Manager / Admin: aprobar / rechazar / cancelar (process)
        ↓
3. Cotizar por ítem × supplier (precio, IVA, adjuntos, vínculo N:M)
        ↓
4. Aceptar cotización(es) para compra
        ↓
5. Enviar a revisión contable → comparativa de cotizaciones
        ↓
6. Enviar a revisión de gerencia
        ↓
7. Gerencia aprueba → genera OC(s) por supplier + IMI/IR si aplica
```

---

## Paso a paso (endpoints)

| Paso | Acción | Método | Ruta relativa |
|:---:|---|:---:|---|
| 1 | Registrar solicitud | `POST` | `/purchase-requests` |
| 2 | Aprobar / rechazar / cancelar | `POST` | `/purchase-requests/{id}/process` |
| 3 | Registrar cotizaciones | `POST` | `/quotations` |
| 3b | Actualizar cotización | `PATCH` | `/quotations/{quotation_id}` |
| 4 | Aceptar cotización | `PATCH` | `/quotations/{quotation_id}/accept-for-purchase` |
| 5 | Enviar a contabilidad | `POST` | `/purchase-requests/{id}/send-accounting-review` |
| 5b | Detalle revisión contable (comparativa) | `GET` | `/requisition-accounting-reviews/{id}` |
| 6 | Enviar a gerencia | `POST` | `/requisition-accounting-reviews/{id}/send-management-review` |
| 6b | Detalle revisión gerencia | `GET` | `/requisition-management-reviews/{id}/details` |
| 7 | Aprobar gerencia y emitir OC | `POST` | `/purchase-orders/{requisition_management_review_id}/process` |

Documentación detallada por endpoint:

- [RegisterPurchaseRequestDocs.md](./RegisterPurchaseRequestDocs.md)
- [RegisterQuotationDocs.md](./RegisterQuotationDocs.md)
- [UpdateQuotationDocs.md](./UpdateQuotationDocs.md)
- [AcceptQuotationForPurchaseDocs.md](./AcceptQuotationForPurchaseDocs.md)
- [ProcessPurchaseOrderTaxesDocs.md](./ProcessPurchaseOrderTaxesDocs.md)
- [GetPurchaseRequestDetailsDocs.md](./GetPurchaseRequestDetailsDocs.md)

---

## Reglas de negocio clave para UI

### Producto–proveedor (N:M)

- Un producto puede tener N proveedores (`supplier_products`).
- Un proveedor puede tener N productos.
- Al crear la solicitud con producto nuevo, se pueden vincular suppliers de una vez.
- Al cotizar con un supplier no vinculado, el backend puede crear el vínculo (`create_supplier_product_if_missing`, default `true`).

### Precios en cotización (backend calcula)

1. Si existe precio preferencial vigente (`tier`): cantidad ≥ `min_quantity` y fecha dentro de `valid_from` / `valid_to` → usa `preferential_price`.
2. Si no → usa `unit_price` del vínculo `SupplierProduct`.
3. Si el vínculo no tiene precio → el frontend **debe** enviar `price_unit`.
4. `price_total` = `quantity × price_unit` (subtotal **sin** IVA).
5. `iva` = monto del impuesto; `0` si `product.is_tax_exempt == true`.

### Retenciones IMI / IR (al emitir OC)

- Se aplican si `total_nio >= 1000` **y** el supplier **no** es exento (`is_tax_exempt == false`).
- Tasas desde catálogo `ValidityDeductions` (`Iva=6`, `Imi=7`, `Ir=8`, `ExchangeRate=3`).
- Se genera **una orden de compra por supplier** a partir de las cotizaciones aceptadas.

---

## Comparativa en Finanzas / Gerencia

Usar el detalle de solicitud o de revisión contable/gerencia. Cada ítem incluye:

- `product_details.supplier_products[]` — suppliers vinculados al producto.
- `quotations[]` — cotizaciones activas con precios, delivery, garantía, inventario, método de pago y adjuntos.

UI sugerida: filas = productos, columnas = suppliers cotizados.

---

## Enums útiles

| Enum | Valores |
|---|---|
| `ProductUsageType` | `1` Insumo, `2` OperationalUse |
| `PaymentMethodType` | `1` ACH, `2` LocalTransfer, `3` Check, `4` Cash, `5` InternationalWire |
| `PurchaseRequestType` | Según catálogo Core (ej. Requisición) |
| `PriorityLevel` | Según catálogo Core (`None` no válido en Requisición) |
| `ManagementReviewStatus` | `Pending` no válido al procesar; usar `Approved` / `Rejected` |

---

## Roles (resumen)

| Acción | Operator | Manager | Admin | Supervisor |
|---|:---:|:---:|:---:|:---:|
| Crear solicitud / cotizar | Sí | Sí | Sí | No |
| Aprobar solicitud / emitir OC | No | Sí | Sí | No |
| Consultar detalle / listados | Sí* | Sí* | Sí | Sí |

\* Alcance: Operator = propias; Manager = su área; Admin/Supervisor = todas.
