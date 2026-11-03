# Asignaciones Operativas

## Crear Asignación Operativa

Endpoint para crear una nueva asignación operativa vinculada a una orden operativa.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments` |
| **Descripción** | Crea una asignación operativa con mercancía, tipo de destino, observaciones, personal/maquinaria externa y opcionalmente maquinaria y colaboradores propios asignados. `user_id`, `company_id`, `module_code` y `operational_order_id` se toman del token y la ruta (no se envían en el body). |
| **Serialización** | El JSON usa `SnakeCaseLower`; los enums viajan como **string** (`JsonStringEnumConverter`). |

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
| `warehouse_id` | `guid` | Condicional | Identificador del almacén destino. **Requerido** si `destination_type` es `Warehouse`. |
| `observations` | `string` | Sí | Comentario para el bodeguero. No puede ser vacío. |
| `merchandise` | `string` | No | Nombre de la mercancía. |
| `merchandise_description` | `string` | No | Descripción de la mercancía. |
| `destination_type` | `enum (DestinationType)` | Sí | Tipo de destino: `None` (0), `Warehouse` (1), `CustomYard` (2), `CustomSheld` (3). |
| `has_assigned_machinery` | `bool` | No | Indica si se envía maquinaria propia asignada (default: `false`). |
| `has_assigned_collaborators` | `bool` | No | Indica si se envían colaboradores propios asignados (default: `false`). |
| `has_outsourced_crew` | `bool` | **Sí** | Indica si la asignación cuenta con personal externo (cuadrilla). |
| `has_outsourced_machinery` | `bool` | **Sí** | Indica si la asignación cuenta con maquinaria externa. |
| `outsourced_crew_count` | `int` | Condicional | Cantidad de personal externo. **Requerido y debe ser mayor que `0`** si `has_outsourced_crew` es `true`. |
| `outsourced_machinery_supplier` | `string` | Condicional | Proveedor de maquinaria externa. **Requerido y no vacío** si `has_outsourced_machinery` es `true`. |
| `assigned_machineries` | `array[AssignedMachinery]` | No | Lista de maquinaria propia asignada. |
| `assigned_machineries[].concept` | `string` | No | Concepto/descripción de la maquinaria. |
| `assigned_machineries[].machinery_id` | `guid` | Sí* | Identificador de la maquinaria. Requerido si el array no está vacío. |
| `assigned_collaborators` | `array[AssignedCollaborator]` | No | Lista de colaboradores propios asignados. |
| `assigned_collaborators[].collaborator_id` | `guid` | Sí* | Identificador del colaborador. Requerido si el array no está vacío. |
| `assigned_collaborators[].role` | `enum (AssignmentCollaboratorsRoles)` | Sí* | Rol del colaborador: `WarehouseAssistant`, `ForkliftOperator`. Requerido si el array no está vacío. |

\* Requerido dentro del array si el array no está vacío.

> **Reglas de los campos externos:**
> - `has_outsourced_crew` y `has_outsourced_machinery` son **obligatorios** (no se puede omitir ninguno).
> - Si `has_outsourced_crew = true` → `outsourced_crew_count` es obligatorio y debe ser `> 0`.
> - Si `has_outsourced_machinery = true` → `outsourced_machinery_supplier` es obligatorio y no vacío.
> - Si el flag es `false`, `outsourced_crew_count` / `outsourced_machinery_supplier` no se validan.

### Ejemplo de Request

```json
{
  "warehouse_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "observations": "Asignación para descarga de contenedor",
  "merchandise": "Contenedor 40ft",
  "merchandise_description": "Contenedor con mercancía paletizada",
  "destination_type": "Warehouse",
  "has_assigned_machinery": true,
  "has_assigned_collaborators": true,
  "has_outsourced_crew": true,
  "has_outsourced_machinery": true,
  "outsourced_crew_count": 4,
  "outsourced_machinery_supplier": "Maquinarias del Norte S.A.",
  "assigned_machineries": [
    {
      "concept": "Montacargas para descarga",
      "machinery_id": "3fa85f64-5717-4562-b3fc-2c963f66afa7"
    }
  ],
  "assigned_collaborators": [
    {
      "collaborator_id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "role": "WarehouseAssistant"
    },
    {
      "collaborator_id": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
      "role": "ForkliftOperator"
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

Mensajes de validación de los campos externos (`CreateAssignmentValidator`):

| Mensaje |
|---------|
| `El ID de la orden operativa es requerido` |
| `El lugar de destino de la mercaderia es obligatorio` |
| `Indique un comentario para el bodegero` |
| `La bodega destino es obligatoria, designe a la bodega que va esta mercaderia` |
| `Debe indicar si la asignación cuenta con personal externo (cuadrilla).` |
| `Debe indicar si la asignación cuenta con maquinaria externa.` |
| `Debe indicar la cantidad de personal externo cuando has_outsourced_crew es true.` |
| `La cantidad de personal externo debe ser mayor que cero.` |
| `Debe indicar el proveedor de maquinaria externa cuando has_outsourced_machinery es true.` |

### 500 Internal Server Error

Error no controlado del servidor.