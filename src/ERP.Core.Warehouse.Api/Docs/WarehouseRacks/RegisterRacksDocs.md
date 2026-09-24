## Registrar Racks (Creación Masiva / Bulk)

Endpoint para registrar de 1 a 60 racks de forma masiva dentro de una sección de tipo racks (`SectionStorageType.Racks`) de un almacén, en una compañía y módulo determinado.

Todos los racks del lote se crean con la misma cantidad de niveles, polines por nivel, dimensiones métricas y perfil de uso. Por cada rack se autogeneran sus posiciones físicas (matriz de niveles 1..N × polines 1..P), sus coordenadas en el plano cartesiano 2D (`racks_coordinates`), su capacidad volumétrica (`racks_capacities`), y se actualizan en cascada las capacidades de la sección (`section_capacities`) y del almacén (`warehouse_capacities`).

---

## Información General

| Campo | Valor |
|---|---|
| **Método** | `POST` |
| **Endpoint** | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections/{section_id}/racks` |
| **Descripción** | Registra hasta 60 racks activos en la sección, autogenera sus posiciones y coordenadas 2D, y recalcula las capacidades en cascada. |

---

## Parámetros de Ruta (Path Params)

| Parámetro | Tipo | Requerido | Descripción |
|:---:|:---:|:---:|---|
| `company_id` | `guid` | Sí | Identificador único de la compañía. |
| `module_code` | `string` | Sí | Código del módulo (ej. `WAREHOUSE`). |
| `warehouse_id` | `guid` | Sí | Identificador único del almacén. |
| `section_id` | `guid` | Sí | Identificador único de la sección receptora. |

---

## Headers

| Header | Valor | Requerido |
|---|---|:---:|
| `Authorization` | `Bearer {token}` | Sí |

---

## Request Body

> **Nota:** `user_id`, `company_id`, `module_code`, `warehouse_id` y `section_id` se extraen automáticamente del token JWT y de la ruta (`[JsonIgnore]`). No deben enviarse en el cuerpo de la petición.

El código de cada rack **no se envía**: es autogenerado por el `CodeGenerator` con la nomenclatura `{SectionCode}-RACK-{NN}` (ej. `SECTION_A003-RACK-01`), de forma secuencial y única dentro de la sección.

| Campo | Tipo | Requerido | Descripción / Reglas |
|---|---|:---:|---|
| `quantity` | `integer` | Sí | Cantidad de racks a crear en esta hilera/bloque. Mínimo `1`, máximo `60`. |
| `row_number` | `integer` | Sí | Número de hilera o pasillo físico en el almacén (ej. `1`). Mayor a `0`. |
| `level_number` | `integer` | Sí | Cantidad de niveles en altura del rack (ej. `2` o `4`). Entre `1` y `10`. |
| `max_pulleys` | `integer` | Sí | Máximo de polines / huecos de palet por nivel (ej. `2`). Entre `1` y `10`. |
| `width` | `decimal` | Sí | Ancho o profundidad frontal del rack en metros (ej. `1.07`). Mayor a `0`. |
| `length` | `decimal` | Sí | Largo longitudinal del módulo del rack en metros (ej. `2.44`). Mayor a `0`. |
| `height` | `decimal` | No | Altura total de la columna del rack en metros (ej. `4.50`). Mayor a `0` si se envía. |
| `usage_profile` | `string` / `int` | Sí | Perfil operativo del rack: `"ActiveFlow"` (`1`) o `"StaticHold"` (`2`). |
| `initial_position_x` | `decimal` | Sí | Coordenada X inicial en metros del primer rack dentro de la sección. Mayor o igual a `0`. |
| `initial_position_y` | `decimal` | Sí | Coordenada Y en metros dentro de la sección (eje vertical del plano). Mayor o igual a `0`. |
| `spacing_x` | `decimal` | Sí | Separación horizontal entre inicios de cada rack contiguo en metros (ej. `2.54` para racks de 2.44m de largo + 10cm de poste/holgura). Mayor a `0`. |

### Ejemplo 1: Racks de Pasillo Central (ActiveFlow - 2 Niveles, 2 Polines)
```json
{
  "quantity": 24,
  "row_number": 1,
  "level_number": 2,
  "max_pulleys": 2,
  "width": 1.07,
  "length": 2.44,
  "height": 4.50,
  "usage_profile": "ActiveFlow",
  "initial_position_x": 2.50,
  "initial_position_y": 6.50,
  "spacing_x": 2.54
}
```

### Ejemplo 2: Racks de Zona de Abandono (StaticHold - 4 Niveles, 2 Polines)
```json
{
  "quantity": 5,
  "row_number": 1,
  "level_number": 4,
  "max_pulleys": 2,
  "width": 1.07,
  "length": 2.44,
  "height": 8.00,
  "usage_profile": "StaticHold",
  "initial_position_x": 1.50,
  "initial_position_y": 3.00,
  "spacing_x": 2.60
}
```

---

## Posiciones Generadas por Rack

Por cada rack creado se genera automáticamente una matriz de `level_number × max_pulleys` posiciones en la tabla `rack_positions`:

| Regla / Campo | Valor Asignado |
|---|---|
| **Nivel inicial** | **Nivel 1** (`N1`). En racks los niveles se cuentan `1..N`. El nivel 0 no existe ya que corresponde a piso / tramos. |
| **Formato de Código** | `{rackCode}-N{nivel}P{polín}`.<br>Ejemplos para rack `SECTION_A003-RACK-01`:<ul><li>`SECTION_A003-RACK-01-N1P1` (Nivel 1, Polín 1)</li><li>`SECTION_A003-RACK-01-N1P2` (Nivel 1, Polín 2)</li><li>`SECTION_A003-RACK-01-N2P1` (Nivel 2, Polín 1)</li><li>`SECTION_A003-RACK-01-N2P2` (Nivel 2, Polín 2)</li></ul> |
| **Row** | Mismo número de hilera del rack (`request.RowNumber`). |
| **Column** | Índice del polín horizontal (`1..MaxPulleys`). |
| **Level** | Nivel vertical de altura (`1..LevelNumber`). |
| **Status** | `Available` (`1`). |
| **AllowsStocking** | `true`. |

---

## Coordenadas para Plano 2D

Para la renderización en el canvas 2D del visualizador de almacén, se calculan e insertan automáticamente las coordenadas en `racks_coordinates`:

$$PositionX_i = InitialPositionX + (i \times SpacingX) \quad \text{para } i \in [0, Quantity - 1]$$
$$PositionY_i = InitialPositionY$$
$$PositionZ = 0, \quad RotationY = 0$$

---

## Respuestas

### ✅ 201 Created

Los racks, sus posiciones, coordenadas y capacidades fueron creados exitosamente. La respuesta HTTP es `201 Created` sin cuerpo.

---

### ❌ 400 Bad Request

Se retorna cuando no se cumplen las validaciones de acceso, tipos de almacenamiento o reglas de negocio:

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "Esta sección no admite racks (tipo de almacenamiento no coincide).",
    "error_codes": ["ERP:SECTION_STORAGE_MISMATCH"]
  },
  "created_at": "2026-09-23 17:30:00"
}
```

#### Catálogo de Errores de Negocio:

| Código de Error | Causa |
|---|---|
| `ERP:SECTION_NOT_FOUND` | La sección especificada no existe, está inactiva o fue eliminada. |
| `ERP:SECTION_WAREHOUSE_MISMATCH` | La sección no pertenece al `warehouse_id` indicado en la ruta. |
| `ERP:SECTION_TYPE_NOT_ALLOWED_FOR_RACKS` | La sección es de tipo pasillo (`SectionType.Aisle`). |
| `ERP:SECTION_STORAGE_MISMATCH` | La sección tiene un tipo de almacenamiento distinto a `SectionStorageType.Racks`. |
| `ERP:RACK_CODE_GENERATION_FAILED` | No fue posible obtener la secuencia de códigos para los racks. |
| `ERP:SECTION_CAPACITY_NOT_FOUND` | La sección no cuenta con registro de capacidad base para recalcular en cascada. |

---

### ❌ 500 Internal Server Error

```json
{
  "status": 500,
  "error": {
    "type_error": "InternalServerError",
    "description": "Ocurrió un error inesperado al procesar la solicitud."
  },
  "created_at": "2026-09-23 17:30:00"
}
```

---

## Catálogos de Enums Utilizados

### `RackUsageProfile`
| Valor Numérico | Nombre (PascalCase) | Descripción |
|:---:|---|---|
| `1` | `ActiveFlow` | Alta rotación / flujo continuo. Ubicado habitualmente en pasillos centrales de preparación rápida. |
| `2` | `StaticHold` | Almacenamiento estático prolongado / resguardo / zona de abandono legal. |

### `RackStatus`
| Valor Numérico | Nombre (PascalCase) | Descripción |
|:---:|---|---|
| `1` | `Available` | Disponible para asignación y colocación de stock. |
| `2` | `Occupied` | Ocupado con mercadería asignada. |
| `3` | `UnderMaintenance` | Fuera de servicio por mantenimiento preventivo o reparación física. |
| `4` | `Blocked` | Bloqueado administrativamente o por motivos de seguridad. |
