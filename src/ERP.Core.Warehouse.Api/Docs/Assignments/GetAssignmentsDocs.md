# Asignaciones Operativas

## Listar Asignaciones Operativas

Endpoint para obtener la lista paginada de asignaciones operativas de una orden operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `GET` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/assignments` |
| **Descripción** | Retorna las asignaciones operativas paginadas con filtros opcionales. Requiere autenticación con token. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |

---

## Parámetros de Consulta (Query Params)

| Parámetro | Tipo | Requerido | Default | Descripción |
|-----------|------|-----------|---------|-------------|
| `operational_order_id` | `guid` | No | - | Identificador único de la orden operativa. Si se omite, retorna las asignaciones de todas las órdenes accesibles. |
| `page_number` | `int` | No | `1` | Número de página. |
| `page_size` | `int` | No | `10` | Tamaño de página (máx. recomendado 100). |
| `status` | `enum (AssignmentOperationalStatus)` | No | - | Filtrar por estado de la asignación. Valores: `None` (0), `Pending` (1), `InProgress` (2), `OnHold` (3), `Downloaded` (4). |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |

---

## Respuestas

### 200 OK

Lista paginada de asignaciones operativas.

```json
{
  "items": [
    {
      "assignmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "operationalOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "isAlerted": false,
      "destinationType": 1,
      "status": 1,
      "merchandise": "Mercancía A",
      "merchandiseDescription": "Descripción de la mercancía",
      "hasMachineryAssigned": true,
      "hasCollaboratorsAssigned": false,
      "createdAt": "2025-01-15T10:30:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalRecords": 50
}
```

**Campos de `AssignmentOperationalDto`:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `assignmentId` | `guid` | Identificador único de la asignación. |
| `operationalOrderId` | `guid` | Identificador de la orden operativa padre. |
| `isAlerted` | `bool` | Indica si la asignación tiene alerta. |
| `destinationType` | `enum (DestinationType)` | Tipo de destino: `None` (0), `Warehouse` (1), `CustomYard` (2), `CustomSheld` (3). |
| `status` | `enum (AssignmentOperationalStatus)` | Estado: `None` (0), `Pending` (1), `InProgress` (2), `OnHold` (3), `Downloaded` (4). |
| `merchandise` | `string` | Nombre de la mercancía. |
| `merchandiseDescription` | `string` | Descripción de la mercancía. |
| `hasMachineryAssigned` | `bool` | Indica si tiene maquinaria asignada. |
| `hasCollaboratorsAssigned` | `bool` | Indica si tiene colaboradores asignados. |
| `createdAt` | `datetime` | Fecha de creación (UTC). |

### 400 Bad Request

Error de validación (parámetros inválidos, paginación incorrecta, etc.).

### 500 Internal Server Error

Error no controlado del servidor.