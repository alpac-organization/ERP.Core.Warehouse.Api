# Asignaciones Operativas

## Crear Asignación Operativa

Endpoint para crear una nueva asignación operativa vinculada a una orden operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments` |
| **Descripción** | Crea una asignación operativa con mercancía, tipo de destino, observaciones, y opcionalmente maquinaria y colaboradores asignados. `user_id`, `company_id`, `module_code` y `operational_order_id` se toman del token y la ruta (no se envían en el body). |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `operational_order_id` | `guid` | Sí | Identificador único de la orden operativa. |

---

## Headers

| Header | Valor | Requerido |
|--------|-------|-----------|
| `Authorization` | `Bearer {token}` | Sí |
| `Content-Type` | `application/json` | Sí |

---

## Request Body

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `warehouseId` | `guid` | No | Identificador del almacén destino (si `destinationType` es `Warehouse`). |
| `observations` | `string` | No | Observaciones adicionales. |
| `merchandise` | `string` | No | Nombre de la mercancía. |
| `merchandiseDescription` | `string` | No | Descripción de la mercancía. |
| `destinationType` | `enum (DestinationType)` | No | Tipo de destino: `None` (0), `Warehouse` (1), `CustomYard` (2), `CustomSheld` (3). |
| `hasAssignedMachinery` | `bool` | No | Indica si se envía maquinaria asignada (default: `false`). |
| `hasAssignedCollaborators` | `bool` | No | Indica si se envían colaboradores asignados (default: `false`). |
| `assignedMachineries` | `array[AssignedMachinery]` | No | Lista de maquinaria asignada. |
| `assignedMachineries[].concept` | `string` | No | Concepto/descripción de la maquinaria. |
| `assignedMachineries[].machineryId` | `guid` | Sí* | Identificador de la maquinaria. Requerido si `hasAssignedMachinery` es `true`. |
| `assignedCollaborators` | `array[AssignedCollaborator]` | No | Lista de colaboradores asignados. |
| `assignedCollaborators[].collaboratorId` | `guid` | Sí* | Identificador del colaborador. Requerido si `hasAssignedCollaborators` es `true`. |
| `assignedCollaborators[].role` | `enum (AssignmentCollaboratorsRoles)` | Sí* | Rol del colaborador. Requerido si `hasAssignedCollaborators` es `true`. |

\* Requerido dentro del array si el flag correspondiente es `true`.

### Ejemplo de Request

```json
{
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "observations": "Asignación para descarga de contenedor",
  "merchandise": "Contenedor 40ft",
  "merchandiseDescription": "Contenedor con mercancía paletizada",
  "destinationType": 1,
  "hasAssignedMachinery": true,
  "hasAssignedCollaborators": true,
  "assignedMachineries": [
    {
      "concept": "Montacargas para descarga",
      "machineryId": "3fa85f64-5717-4562-b3fc-2c963f66afa7"
    }
  ],
  "assignedCollaborators": [
    {
      "collaboratorId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "role": 1
    },
    {
      "collaboratorId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
      "role": 2
    }
  ]
}
```

---

## Respuestas

### 201 Created

Asignación creada correctamente. Retorna `CreatedResult` sin cuerpo (o `true` según implementación).

### 400 Bad Request

Validaciones de entrada, orden operativa no encontrada, maquinaria/colaborador no existen, sin permiso, etc. (`ErrorResponse`).

### 500 Internal Server Error

Error no controlado del servidor.