# Asignaciones Operativas

## Asignar Posiciones (Tramos y Racks) — FUSIONADO

> **NOTA IMPORTANTE:** el `POST /api/v1/companies/{company_id}/modules/{module_code}/operational-orders/{operational_order_id}/assignments/{assignment_id}/assignment-positions` fue **eliminado**. Su lógica (reserva de posiciones, histórico en `assignment_stock_placements` y generación de códigos QR/barcode) quedó fusionada en el **PATCH del mismo endpoint**.

Ver: [UpdateAssignmentPositionsDocs.md](./UpdateAssignmentPositionsDocs.md) — **Asignar Posiciones e Información de Polines (PATCH)**.

## Contrato de posiciones (se mantiene en el PATCH fusionado)

La sección `sections` del body del PATCH conserva exactamente el contrato del antiguo POST:

| Parámetro                       | Tipo      | Requerido | Descripción |
|---------------------------------|-----------|:---------:|-------------|
| `sections`                      | `array`   | Sí        | Lista de secciones con las posiciones a asignar. Mínimo una. |
| `sections[].section_id`         | `guid`    | Sí        | Id de la sección. |
| `sections[].tramos`             | `array`   | No        | Bloques de tramos de la sección. Solo válido si la sección es de tipo `Lots`. |
| `sections[].tramos[].block_id`  | `guid`    | Sí        | Id del tramo (entidad `Lots`). |
| `sections[].tramos[].position_ids` | `guid[]` | Sí       | Ids de las posiciones del tramo a asignar. Mínimo uno. |
| `sections[].racks`               | `array`   | No        | Bloques de racks de la sección. Solo válido si la sección es de tipo `Racks`. |
| `sections[].racks[].block_id`   | `guid`    | Sí        | Id del rack (entidad `Racks`). |
| `sections[].racks[].position_ids` | `guid[]` | Sí       | Ids de las posiciones a asignar. Mínimo una. |

```json
{
  "merchandise_type": 2,
  "pallets": [
    { "type": 1, "count_pallets": 8 }
  ],
  "sections": [
    {
      "section_id": "f8a964a3-76a0-4fc7-bf98-251f28b4d081",
      "tramos": [
        {
          "block_id": "e2a48b32-0001-4444-8888-abcdef012345",
          "position_ids": [
            "3b2e591c-1111-4444-9999-012345abcdef"
          ]
        }
      ],
      "racks": []
    }
  ]
}
```

## Cambios de comportamiento respecto al POST original

| Regla | Antes (POST) | Ahora (PATCH fusionado) |
|---|---|---|
| Estado requerido | `Pending` (obligatorio). | `InProgress` (obligatorio). El paso `Pending → InProgress` lo hace `POST .../start-task`. |
| Transición de estado | `Pending → InProgress` al reservar. | **No** cambia el estado. |
| Generación de códigos | Siempre. | Solo si viene `sections`. |
| Respuesta | `200` DTO con códigos. | `200` DTO con códigos si vino `sections`; `204` si solo vino `merchandise_type`/`pallets`. |
| Posiciones `Available` | Requerido. | Igual. |
| Histórico `assignment_stock_placements` | Se registra por posición. | Igual. |
| Validaciones de sección/almacén | Iguales. | Iguales. |

## `POST /merchandise` (AssignmentMerchandiseController)

Se **conserva** porque otros servicios lo consumen. Comparte el mismo handler fusionado:

- **Request**: igual (`{ "sections": [...] }`).
- **Response**: igual (`code_qr` + `code_bar` al asignar posiciones).
- **Comportamiento**: ahora exige `InProgress` (debe llamarse `POST .../start-task` antes). No cambia el estado.

## Catálogos de Enums Utilizados

### `RackStatus`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Available` | Lista y libre para asignar mercadería. **Requerido** para asignar. |
| `2` | `Occupied` | Tiene mercadería asignada actualmente. |
| `3` | `UnderMaintenance` | Fuera de servicio por mantenimiento. |
| `4` | `Blocked` | Inhabilitado por otra causa. |
| `5` | `Reserved` | Apartado para una operación en curso. **Estado resultante** tras asignar. |

### `SectionStorageType`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `1` | `Racks` | Sección de racks: recibe bloques en `racks`. |
| `2` | `Lots` | Sección de tramos: recibe bloques en `tramos`. |
| `3` | `Pallets` | No soportado para este endpoint. |
| `4` | `None` | No soportado para este endpoint. |

### `CodesType`

| Valor | Nombre | Descripción |
|:---:|---|---|
| `0` | `None` | Sin tipo. |
| `1` | `Qr` | Código QR del voucher. |
| `2` | `Bar` | Código de barras del voucher. |