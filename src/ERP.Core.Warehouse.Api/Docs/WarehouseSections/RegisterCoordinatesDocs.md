# Almacenes - Coordenadas de Posiciones

## Registrar Coordenadas de Posiciones (Lotes y Racks)

Endpoint para registrar o actualizar las coordenadas 3D (X, Y, Z, Rotación Y) de posiciones de lotes y/o racks dentro de una sección de un almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/coordinates` |
| **Descripción** | Permite registrar coordenadas para posiciones de lotes (`LotsPositions`) y/o posiciones de racks (`RackPositions`) en una sección. `warehouse_id` y `section_id` se toman de la ruta. El `targetType` define qué tipo de posiciones se están enviando. `user_id`, `company_id`, `module_code` se toman del token y la ruta. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---------:|:----:|-----------|-------------|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `section_id` | `guid` | Sí | Identificador único de la sección. |

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
| `lotId` | `guid` | No | Identificador del lote (opcional, para contexto). |
| `rackId` | `guid` | No | Identificador del rack (opcional, para contexto). |
| `targetType` | `enum (CoordinateTargetType)` | **Sí** | Tipo de posiciones a registrar: `LotsPositions` (1) o `RackPositions` (2). |
| `lotsPositionsInformation` | `array[LotsPositionsInformation]` | Sí* | Lista de coordenadas para posiciones de lotes. Requerido si `targetType = 1`. |
| `lotsPositionsInformation[].lotPositionId` | `guid` | Sí | Identificador de la posición del lote. |
| `lotsPositionsInformation[].positionX` | `decimal` | Sí | Coordenada X (horizontal). |
| `lotsPositionsInformation[].positionY` | `decimal` | Sí | Coordenada Y (vertical/altura). |
| `lotsPositionsInformation[].positionZ` | `decimal` | Sí | Coordenada Z (profundidad). |
| `lotsPositionsInformation[].rotationY` | `decimal` | Sí | Rotación en eje Y (grados). |
| `rackPositionsInformation` | `array[RackPositionsInformation]` | Sí* | Lista de coordenadas para posiciones de racks. Requerido si `targetType = 2`. |
| `rackPositionsInformation[].rackPositionId` | `guid` | Sí | Identificador de la posición del rack. |
| `rackPositionsInformation[].positionX` | `decimal` | Sí | Coordenada X (horizontal). |
| `rackPositionsInformation[].positionY` | `decimal` | Sí | Coordenada Y (vertical/altura). |
| `rackPositionsInformation[].positionZ` | `decimal` | Sí | Coordenada Z (profundidad). |
| `rackPositionsInformation[].rotationY` | `decimal` | Sí | Rotación en eje Y (grados). |

\* Al menos uno de los dos arrays debe tener datos según el `targetType` seleccionado.

### `CoordinateTargetType`

| Valor | Nombre | Descripción |
|-------|--------|-------------|
| `1` | `LotsPositions` | Coordenadas para posiciones de lotes. |
| `2` | `RackPositions` | Coordenadas para posiciones de racks. |

### Ejemplo de Request (Lotes - `targetType: 1`)

```json
{
  "lotId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetType": 1,
  "lotsPositionsInformation": [
    {
      "lotPositionId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "positionX": 10.50,
      "positionY": 0.00,
      "positionZ": 5.25,
      "rotationY": 0.00
    },
    {
      "lotPositionId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "positionX": 12.50,
      "positionY": 0.00,
      "positionZ": 5.25,
      "rotationY": 90.00
    }
  ],
  "rackPositionsInformation": []
}
```

### Ejemplo de Request (Racks - `targetType: 2`)

```json
{
  "rackId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
  "targetType": 2,
  "lotsPositionsInformation": [],
  "rackPositionsInformation": [
    {
      "rackPositionId": "3fa85f64-5717-4562-b3fc-2c963f66afb0",
      "positionX": 0.00,
      "positionY": 0.00,
      "positionZ": 0.00,
      "rotationY": 0.00
    },
    {
      "rackPositionId": "3fa85f64-5717-4562-b3fc-2c963f66afb1",
      "positionX": 3.00,
      "positionY": 0.00,
      "positionZ": 0.00,
      "rotationY": 0.00
    }
  ]
}
```

---

## Respuestas

### 200 OK

Coordenadas registradas/actualizadas correctamente. Retorna `OkResult` sin cuerpo (o `true` según implementación).

### 400 Bad Request

Validaciones de entrada: `targetType` inválido, arrays vacíos para el tipo seleccionado, posiciones no encontradas, sección no pertenece al almacén, sin permiso, etc. (`ErrorResponse`).

### 500 Internal Server Error

Error no controlado del servidor.