# Detalle de Posición

Endpoint para consultar el detalle de una **posición de almacén** (`LotsPositions` o `RackPositions`) identificada por su `id`. A partir de la posición se ubica su `assignment_stock_placement`, se obtiene la asignación operativa, la orden operativa (PO), la recepción asociada y las **posiciones restantes** de la misma asignación.

- **Detalle de PO**: `OperationalOrderDetailsDto` completo (PoCode, DocumentNumber, DocumentType, Status, ShippingCompany, Consignee, Sender, Weight, PackagesCount, Description, IsAlerted, IsConsolidated, etc.) con `CustomerInformation`, `CostCenterInformation` y `ReceptionEntranceInformation`.
- **Detalle de Recepción**: `ReceptionEntranceDetailsDto` (ReceptionCode, SealNumber, ContainerNumber, CountryOfOrigin, DocumentType, tiempos de salida, transporte y aduana).
- **Fecha de ingreso de la recepción**: campo `created_at` de `reception_entrance_information` (equivale a `ReceptionEntrance.CreatedAt`).
- **Resto de posiciones**: `remaining_positions` = las posiciones registradas en `assignment_stock_placements` **del mismo `assignment_id`**, excluyendo la posición consultada (mismas clases que `GetMerchandiseLocationDetails`).

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/positions/{position_id}/position-detail` |
| **Descripción** | Obtiene el detalle de PO, recepción, fecha de ingreso y posiciones restantes de la asignación a la que pertenece una posición. |
| **Tags**        | `Asignaciones Operativas` |

---

## Parámetros de Ruta (Path Params)

| Parámetro              | Tipo     | Requerido | Descripción |
|:----------------------:|:--------:|:---------:|-------------|
| `company_id`           | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`          | `string` | Sí        | Código del módulo dentro de la compañía (ej. `WAREHOUSE`). |
| `position_id`          | `guid`   | Sí        | Identificador de la posición (`LotsPositions.Id` o `RackPositions.Id`). |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |
| `x-api-key`     | `{api_key}`        | Sí        |

No requiere body.

---

## Flujo del Handler (`GetPositionDetailHandler`)

1. `ValidateAccessAsync(user_id, company_id, module_code, ct)`: valida acceso del usuario. Si falla devuelve `400`.
2. Busca el `AssignmentStockPlacements` donde `LotPositionId == position_id || RackPositionId == position_id`. Si no existe: `ERP:POSITION_WITHOUT_ASSIGNMENT`.
3. Carga la asignación (tramos/racks/sección) junto con su `OperationalOrder`, incluyendo `Customer`, `Reception.ReceptionTransport` y `CostCenter`.
4. Si no existe la asignación: `ERP:ASSIGNMENT_NOT_FOUND`.
5. Mapea la OP a `OperationalOrderDetailsDto` y los placements restantes (mismo assignment, excluyendo la posición consultada) a `AssignmentStockPlacementsInformation`.

---

## Respuestas

### ✅ 200 OK

```jsonc
{
  "operationalOrderDetail": {
    "operationOrderId": "uuid",
    "poCode": "string",
    "documentNumber": "string",
    "documentType": 0,
    "status": 2,
    "isAlerted": false,
    "isConsolidated": false,
    "shippingCompany": "string",
    "consignee": "string",
    "sender": "string",
    "weight": 0,
    "packagesCount": 0,
    "description": "string",
    "policyNumber": "string",
    "customerInformation": {},
    "costCenterInformation": {},
    "receptionEntranceInformation": {
      "receptionEntranceId": "uuid",
      "receptionCode": "string",
      "vehiclePlateNumber": "string",
      "containerNumber": "string",
      "countryOfOrigin": "string",
      "sealNumber": "string",
      "documentType": 0,
      "vehicleExitTime": "12:30:00",
      "containerExitTime": "14:00:00",
      "receptionTransportEntranceInformation": {},
      "additionalData": "{}",
      "customBranchesInformation": {},
      "createdAt": "2026-10-05T08:30:00"          // ← Fecha de ingreso de la recepción
    }
  },
  "remainingPositions": [
    {
      "sectionInformation": {},
      "lotPositionInformation": { "positionCode": "string", "row": 0, "column": 0, "level": 0, "status": 5 },
      "rackPositionInformation": {}
    }
  ],
  "codeQr": "https://s3.../qr.png",        // Codes[Qr].ImageUrl
  "codeBar": "https://s3.../bar.png",       // Codes[Bar].ImageUrl
  "qrCode": "uuid-uuid",                     // Codes[Qr].CodeGenerated
  "barCode": "uuid-uuid",                    // Codes[Bar].CodeGenerated
  "merchandiseInformation": {
    "merchandise": "string",
    "merchandiseDescription": "string",
    "hasMerchandiseDescription": false,
    "category": null,
    "merchandiseType": 1,
    "destinationType": 1,
    "observations": "string",
    "hasPositionatingInformation": true,
    "pallets": [
      { "countPallets": 4, "type": 1, "width": 1.1, "length": 1.2, "bulksPerPallet": null }
    ]
  }
}
```

`remaining_positions` usa la misma estructura que `AssignmentStockPlacementsInformation` de `GetMerchandiseLocationDetails`. `codeQr`/`codeBar` corresponden a los `ImageUrl` de la entidad `Codes` (tipo `Qr`/`Bar`), y `qrCode`/`barCode` a sus valores `CodeGenerated`. `merchandiseInformation` trae la mercadería de la entidad de asignamiento, incluyendo los polines (`pallets`) deserializados de `AdditionalData`.

### ❌ 400 Bad Request

| `typeError` | `description` |
|-------------|---------------|
| `ERP:POSITION_WITHOUT_ASSIGNMENT` | `La posición no tiene asignaciones asociadas.` |
| `ERP:ASSIGNMENT_NOT_FOUND` | `No se encontró la asignación asociada a la posición.` |
| `Validation_Error` | Errores del `GetPositionDetailValidator`. |
| `ERP:003` | `Este usuario no existe!` |
| `ERP:02` | Usuario bloqueado temporalmente. |
| `ERP:03` | Usuario inactivo. |
| `ERP:004` | `No existe un perfil asociado a esta empresa` |
| `ERP:005` | `No tienes acceso a este módulo` |
| `ERP:006` | El rol asignado no es válido. |

### ❌ 403 Forbidden

| `typeError` | `description` |
|-------------|---------------|
| `Forbidden` | `x-api-key` ausente o incorrecta. Lo devuelve el `ApiKeyMiddleware` antes de llegar al controller. |

### ❌ 500 Internal Server Error

Para excepciones **no controladas** (fuera de `CoreException`) el `ExceptionMiddleware` responde en **PascalCase**:

```json
{
  "Status": 500,
  "Error": {
    "TypeError": "Server_Error",
    "Description": "Error interno no controlado."
  },
  "CreatedAt": "2026-10-08 14:32:10"
}
```

---

## Notas

| Regla | Descripción |
|---|---|
| Identificación | La posición es de tramo (`LotsPositions`) o de rack (`RackPositions`); el placement puede tener ambos ids. |
| `remaining_positions` | Posiciones activas del mismo `assignment_id`, **excluyendo** la posición consultada. |
| `created_at` | Fecha de ingreso de la recepción (`ReceptionEntrance.CreatedAt`), dentro de `receptionEntranceInformation`. |
| `codeQr`/`codeBar` | URLs de la entidad `Codes` (`ImageUrl`) por tipo `Qr`/`Bar`. |
| `qrCode`/`barCode` | Valores `CodeGenerated` de la entidad `Codes` por tipo `Qr`/`Bar`. |
| `merchandiseInformation` | Mercadería de la entidad de asignamiento (`Merchandise`, `MerchandiseDescription`, `Category`, `MerchandiseType`, `DestinationType`, `Observations`, `HasPositionatingInformation` y `pallets`). |
| `pallets` | Lista `PositionatingInformation` deserializada de `AssignmentOperational.AdditionalData`. |
| Reutilización | Reusa `OperationalOrderDetailsDto`, `ReceptionEntranceDetailsDto` y `AssignmentStockPlacementsInformation` (no duplica DTOs). |