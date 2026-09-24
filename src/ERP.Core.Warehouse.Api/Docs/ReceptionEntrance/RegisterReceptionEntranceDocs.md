# Control de Acceso

## Registrar Recepción

Endpoint para registrar la recepción de un vehículo/contenedor (control de acceso) en la entrada de un almacén, junto con su información de transporte, evidencias fotográficas y las órdenes operativas (OP) correspondientes.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/reception-entrances` |
| **Descripción** | Registra una recepción (conducción/entrada), su transporte, sube las evidencias a S3 y genera 1+ órdenes operativas con código consecutivo único. |

---

## Parámetros de Ruta (Path Params)

| Parámetro     | Tipo     | Requerido | Descripción |
|:-------------:|:--------:|-----------|-------------|
| `company_id`  | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code` | `string` | Sí        | Código del módulo dentro de la compañía. |

---

## Headers

| Header          | Valor            | Requerido |
|-----------------|------------------|-----------|
| `Authorization` | `Bearer {token}` | Sí        |

---

## Request Body

El JSON usa **`SnakeCaseLower`** (`PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower`) y es **case-insensitive**. Los enums viajan como **string** (`JsonStringEnumConverter`).

`user_id`, `company_id` y `module_code` no van en el body: se toman del token y de la ruta.

| Parámetro                        | Tipo                            | Requerido | Descripción |
|----------------------------------|---------------------------------|-----------|-------------|
| `evidence_base64`                | `array<string>`                 | No        | Evidencias fotográficas en base64. Cada una se sube a S3 (`Warehouse/ReceptionEntrance`) y su URL queda en `additional_data.evidence_urls`. |
| `general_information`            | `objeto`                        | Sí        | Información general de la recepción. |
| `transport_information`          | `objeto`                        | Sí        | Información del transporte/conductor. |
| `customs_declaration_information`| `objeto` / `null`               | No        | Detalle de la declaración de aduana (solo `document_type = "CustomsDeclaration"`). |

### `general_information`

| Campo                       | Tipo              | Requerido | Descripción |
|-----------------------------|-------------------|-----------|-------------|
| `custom_branch_id`          | `guid`            | Sí        | Identificador de la aduana (`CUSTOM_BRANCH_ID`). |
| `seal_number`               | `string`          | Sí        | Número de precinto / marchamo. |
| `country_origin`            | `string`          | Sí        | País de origen. |
| `container_number`          | `string`          | Sí        | Número de contenedor. |
| `document_type`             | `enum (DocumentType)` | Sí     | `DUCA` o `CustomsDeclaration` (cualquier otro valor responde 400). |
| `ducat_numbers`             | `array<string>`   | Si es DUCA| Números Duca. **Uno por cada OP** (cada duca genera una orden operativa con su propio `OP_CODE`). |
| `customs_declaration_number`| `string`          | Si es CustomsDeclaration | Número de la declaración de aduana. |

### `transport_information`

| Campo                    | Tipo                     | Requerido | Descripción |
|--------------------------|--------------------------|-----------|-------------|
| `driver_name`            | `string`                 | Sí        | Nombre del conductor. |
| `driver_license`         | `string`                 | Sí        | Licencia del conductor. |
| `transportista`          | `string`                 | Sí        | Empresa/conductor transportista. |
| `vehicle_plate_number`   | `string`                 | Sí        | Placa del vehículo. |
| `vehicle_chassis_number` | `string`                 | Sí        | Chasis del vehículo. |
| `transport_unit`         | `enum (TransportUnit)`   | Sí        | `Container` (vehículo articulado) o `Van` (un solo vehículo). |

### `customs_declaration_information`

| Campo                | Tipo     | Requerido | Descripción |
|----------------------|----------|-----------|-------------|
| `total_weight`       | `decimal`| Sí        | Peso total de la mercancía. |
| `package_number`     | `decimal`| Sí        | Número de paquetes/bultos. |
| `product_description`| `string` | Sí        | Descripción del producto. |

### Ejemplo (DUCA)

```json
{
  "evidence_base64": [
    "<base64 de la imagen 1>",
    "<base64 de la imagen 2>"
  ],
  "general_information": {
    "custom_branch_id": "1f2b3c4d-0000-0000-0000-000000000001",
    "seal_number": "PRC-2026-0001",
    "country_origin": "China",
    "container_number": "MSKU1234567",
    "document_type": "DUCA",
    "ducat_numbers": ["DUCA-0001", "DUCA-0002"],
    "customs_declaration_number": null
  },
  "transport_information": {
    "driver_name": "Juan Pérez",
    "driver_license": "L-123456",
    "transportista": "Transportes Nica",
    "vehicle_plate_number": "M-98765",
    "vehicle_chassis_number": "CHS-1234567890",
    "transport_unit": "Container"
  },
  "customs_declaration_information": null
}
```

### Ejemplo (CustomsDeclaration)

```json
{
  "evidence_base64": [],
  "general_information": {
    "custom_branch_id": "1f2b3c4d-0000-0000-0000-000000000001",
    "seal_number": "PRC-2026-0002",
    "country_origin": "México",
    "container_number": "TCLU7654321",
    "document_type": "CustomsDeclaration",
    "ducat_numbers": [],
    "customs_declaration_number": "DA-2026-00321"
  },
  "transport_information": {
    "driver_name": "María López",
    "driver_license": "L-987654",
    "transportista": "Logística Centroamérica",
    "vehicle_plate_number": "M-11223",
    "vehicle_chassis_number": "CHS-0987654321",
    "transport_unit": "Van"
  },
  "customs_declaration_information": {
    "total_weight": 15200.50,
    "package_number": 120,
    "product_description": "Electrodomésticos - Planchas y licuadoras"
  }
}
```

---

## Flujo del Handler

1. `ValidateAccessAsync(user_id, company_id, module_code)`: valida usuario → perfil → módulo → rol. Si falla, responde `400`.
2. Si el rol es **`Supervisor`** → `400` (`ERP:INVALID_ACCESS`): `"No tienes acceso a realizar esta acción"`.
3. Registra la **recepción** (`ReceptionEntrance`) con `IsActive = true`.
4. Sube cada `evidence_base64` a S3 y persiste las URLs en `additional_data.evidence_urls`.
5. Registra el **transporte** (`ReceptionTransportEntrance`).
6. Genera las **órdenes operativas** (`OperationalOrder`):
   - **`DUCA`**: por **cada** número en `ducat_numbers` se genera una nueva OP con `Id` y `OpCode` **únicos** (todos comparten `ReceptionId` y `DocumentType`). `OpCode` se obtiene con `ICodeGenerator.GenerateUniqueOperationalOrderCodeAsync(cost_center_id)`.
   - **`CustomsDeclaration`**: una sola OP con `DocumentNumber = customs_declaration_number`. Si viene `customs_declaration_information`, solo puede registrarse dentro de la ventana aduanera (**5:00 pm – 8:00 am** o **12:00 pm – 1:00 pm**, evaluada con la hora UTC del servidor); fuera de ventana responde `400` (`ERP:CUSTOMS_DECLARATION_OUT_OF_WINDOW`). Se guardan `Weight`, `PackagesCount` y `Description`.
   - Otro `document_type` → `400` (`ERP:INVALID_DOCUMENT`).
7. Un único `SaveChanges` persiste todo al final (sin commits intermedios).

### Notas

| Tema | Detalle |
|---|---|
| Validación | El `CreateReceptionEntranceValidator` se encuentra **vacío**; las reglas de negocio se evalúan en el handler. |
| Enums (string) | `DocumentType` = `DUCA` / `CustomsDeclaration`; `TransportUnit` = `Container` / `Van`. |
| Código de OP | El `op_code` tiene un índice **único** en BD (`ix_operational_orders_op_code`); por eso DUCA genera un código distinto por duca. |
| Evidencia vacía | `evidence_base64 = []` es válido (no genera URLs). |
| Fallo de generación de código | Si `ICodeGenerator` falla en medio del procesamiento, responde `500` y **no** se persiste nada (el error ocurre antes del `SaveChanges`). |

---

## Respuestas

### ✅ 201 Created

La recepción se registró correctamente. El cuerpo de la respuesta va vacío (`CreatedResult`).

### ❌ 400 Bad Request

Usa la entidad `ErrorResponse` (`ERP.Core.Domain.Entities.Errors`):

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "No tienes acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-24 12:00:00"
}
```

Tipos de error documentados:

| Tipo de error | Descripción |
|---|---|
| Acceso / módulo | `"El usuario no existe..."`, `"No tienes acceso a este módulo"`, etc. (de `ValidateAccessAsync`). |
| Rol `Supervisor` | `"No tienes acceso a realizar esta acción"` (`ERP:INVALID_ACCESS`). |
| Ventana aduanera | `"La información de declaración de aduana solo puede registrarse de 5:00 pm a 8:00 am o de 12:00 pm a 1:00 pm"` (`ERP:CUSTOMS_DECLARATION_OUT_OF_WINDOW`). |
| Tipo de documento | `"Error al registrar la información, el tipo de documento no es aceptable"` (`ERP:INVALID_DOCUMENT`). |

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud"
  },
  "created_at": "2026-09-24 12:00:00"
}
```

Fallo al generar el código de la OP: `"Ocurrió un error al generar el código de la orden operativa"` (`ERP:INTERNAL_ERROR`).

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `201` | Recepción registrada exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor o fallo al generar código (`ErrorResponse`). |