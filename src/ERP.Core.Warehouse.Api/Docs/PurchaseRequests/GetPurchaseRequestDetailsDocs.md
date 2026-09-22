# Solicitudes de compras

## Obtener Detalle de Solicitud de Compra

Endpoint para obtener el detalle completo de una solicitud de compra dentro del módulo de purchase.

| Campo | Valor |
|-------|-------|
| **Método**      | `GET` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/purchase-requests/{purchase_request_id}` |
| **Descripción** | Retorna el detalle completo de una solicitud de compra incluyendo información del creador, revisor, sucursal, área solicitante, centro de costos y los items de la solicitud. |

---

## Parámetros de Ruta 

| Parámetro             | Tipo     | Requerido | Descripción |
|:---------------------:|:--------:|-----------|-------------|
| `company_id`          | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`         | `string` | Sí        | Código del módulo dentro de la compañía. |
| `purchase_request_id` | `guid`   | Sí        | Identificador de la solicitud de compra a consultar. Debe existir y estar activa. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Respuestas

### ✅ 200 OK

Retorna el detalle completo de la solicitud de compra.

**Estructura de respuesta (`PurchaseRequestDetailsDto`):**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `observations` | `string \| null` | Observaciones / concepto de la solicitud (mapeado desde `Concept`). |
| `reason_rejection` | `string \| null` | Motivo de rechazo si aplica. |
| `annulment_reason` | `string \| null` | Motivo de anulación si aplica. |
| `creator_user_information` | `UserInformation` | Información del usuario que creó la solicitud. |
| `reviewer_user_information` | `UserInformation \| null` | Información del usuario que revisó la solicitud (si existe). |
| `branch_information` | `BranchInformation` | Información de la sucursal asociada. |
| `information_from_requesting_area` | `WorkAreaInformation` | Información del área solicitante (WorkArea de la solicitud). |
| `cost_center_information` | `CostCenterInformation` | Información del centro de costos asociado. |
| `code` | `string` | Código de la solicitud (ej: `ALP-MGA-REQ-12`). |
| `purchase_request_id` | `guid` | Identificador único de la solicitud. |
| `request_date` | `date` | Fecha de la solicitud (formato `YYYY-MM-DD`). |
| `revision_date` | `date \| null` | Fecha de revisión si aplica. |
| `priority_level` | `string (enum PriorityLevel)` | Nivel de prioridad: `None`, `Low`, `Normal`, `High`, `Critical`. |
| `destination` | `string (enum DestinationRequest)` | Destino: `Internal`, `External`. |
| `request_type` | `string (enum PurchaseRequestType)` | Tipo de solicitud: `Requisition`, `PurchaseOrder`, etc. |
| `request_status` | `string (enum PurchaseRequestStatus)` | Estado: `Pending`, `Approved`, `Rejected`, `Annulled`, `InReview`, `SentToReview`. |

#### UserInformation

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `user_id` | `guid` | Identificador del usuario. |
| `email` | `string` | Email del usuario. |
| `fullname` | `string` | Nombre completo. |
| `picture_url` | `string \| null` | URL de la foto de perfil. |
| `user_status` | `string (enum UserStatus)` | Estado: `Active`, `Inactive`, `Blocked`, `Pending`. |
| `work_area_information` | `WorkAreaInformation \| null` | Área de trabajo del usuario (desde su perfil activo en la compañía). |

#### WorkAreaInformation

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `work_area_id` | `guid` | Identificador del área de trabajo. |
| `work_area_code` | `integer` | Código numérico del área. |
| `description` | `string \| null` | Descripción del área. |
| `work_area_name` | `string` | Nombre del área. |
| `cost_centers` | `array<CostCenterInformation>` | Lista de centros de costos asociados al área. |

#### BranchInformation

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `branch_id` | `guid` | Identificador de la sucursal. |
| `branch_code` | `string` | Código de la sucursal (ej: `ALP-MGA`). |
| `branch_name` | `string` | Nombre de la sucursal. |
| `company_alias` | `string` | Alias de la compañía (ej: `ALPAC`). |

#### CostCenterInformation

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `cost_center_id` | `guid` | Identificador del centro de costos. |
| `description` | `string \| null` | Descripción. |
| `cost_center_name` | `string` | Nombre del centro de costos. |
| `coil_code` | `integer` | Código de coil. |
| `cost_center_code` | `integer` | Código del centro de costos. |

---

### Ejemplo de Respuesta (200 OK)

```json
{
  "observations": "contexto justificando requisicion #1",
  "reason_rejection": null,
  "annulment_reason": null,
  "creator_user_information": {
    "user_id": "f8405520-7e70-4fc5-a034-8508ea73db93",
    "email": "joharyjimenez@example.com",
    "fullname": "Johary Celeste Jimenez Castrillo",
    "picture_url": null,
    "user_status": "Active",
    "work_area_information": {
      "work_area_id": "f006b2d8-af3f-4c8b-b59c-a08b677b66ca",
      "work_area_code": 4,
      "description": "INFORMÁTICA",
      "work_area_name": "INFORMÁTICA",
      "cost_centers": []
    }
  },
  "reviewer_user_information": {
    "user_id": "7293b6b6-3070-402d-8594-34321cfabf07",
    "email": "admin@vassalli.com.ni",
    "fullname": "Administrador",
    "picture_url": null,
    "user_status": "Active",
    "work_area_information": {
      "work_area_id": "f006b2d8-af3f-4c8b-b59c-a08b677b66ca",
      "work_area_code": 4,
      "description": "INFORMÁTICA",
      "work_area_name": "INFORMÁTICA",
      "cost_centers": []
    }
  },
  "branch_information": {
    "branch_id": "f9c8c488-f53e-46c2-9594-1e9b23cf805c",
    "branch_code": "ALP-MGA",
    "branch_name": "Almacenadora del Pacífico S.A.",
    "company_alias": "ALPAC"
  },
  "information_from_requesting_area": {
    "work_area_id": "68a74b56-c820-4a74-bb9f-8faaa0f69907",
    "work_area_code": 3,
    "description": null,
    "work_area_name": "ADMINISTRACION",
    "cost_centers": []
  },
  "cost_center_information": {
    "cost_center_id": "8d8c47fd-337c-41b7-8156-eac944027ff7",
    "description": null,
    "cost_center_name": "GERENCIA DE INFORMATICA",
    "coil_code": 4,
    "cost_center_code": 0
  },
  "code": "ALP-MGA-REQ-12",
  "purchase_request_id": "7063cd0d-62d5-4b32-a032-c40d36e99c19",
  "request_date": "2026-09-21",
  "revision_date": "2026-09-21",
  "priority_level": "Normal",
  "destination": "Internal",
  "request_type": "Requisition",
  "request_status": "Approved"
}
```

---

### ❌ 400 Bad Request

Error de validación, acceso o reglas de negocio usando `ErrorResponse`:

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

### ❌ 404 Not Found

La solicitud de compra no existe o está inactiva:

```json
{
  "status": 404,
  "error": {
    "type_error": "NotFound",
    "description": "No se encontro el detalle de esta solicitud"
  },
  "created_at": "2026-09-10 08:00:00"
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
| `200` | Detalle de solicitud de compra obtenido exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`). |
| `404` | Solicitud de compra no encontrada o inactiva (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |

---