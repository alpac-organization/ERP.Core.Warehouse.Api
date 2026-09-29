# Almacén

## Registrar Sección

Endpoint para registrar una sección dentro de un almacén de una compañía/módulo. El código de la sección se genera automáticamente.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/warehouses/{warehouse_id}/sections` |
| **Descripción** | Registra una sección en el almacén, genera su código (`GenerateUniqueSectionCodeAsync`), calcula y persiste su capacidad (`section_capacity`) y actualiza la capacidad del almacén (`warehouse_capacity`). |

---

## Parámetros de Ruta (Path Params)

| Parámetro      | Tipo     | Requerido | Descripción |
|:--------------:|:--------:|-----------|-------------|
| `company_id`   | `guid`   | Sí        | Identificador único de la compañía. |
| `module_code`  | `string` | Sí        | Código del módulo dentro de la compañía. |
| `warehouse_id` | `guid`   | Sí        | Identificador del almacén donde se registra la sección. Debe existir y estar activo. |

---

## Headers

| Header          | Valor              | Requerido |
|-----------------|--------------------|-----------|
| `Authorization` | `Bearer {token}`   | Sí        |

---

## Request Body

`user_id`, `company_id`, `module_code` y `warehouse_id` no forman parte del JSON: se toman del token y de la ruta (`[JsonIgnore]`). El `code` **no** se envía: lo genera el backend.

| Parámetro                             | Tipo                        | Requerido   | Descripción |
|---------------------------------------|-----------------------------|-------------|-------------|
| `section_type`                        | `enum (SectionType)`        | Sí          | Tipo de sección. Valores: `Storage`, `Aisle`. |
| `section_storage_type`                | `enum (SectionStorageType)` | Sí          | Tipo de almacenaje. Valores: `Racks`, `Lots`, `Pallets`, `None`. |
| `width`                               | `decimal`                   | Sí          | Ancho (metros). Debe ser mayor a cero. Máximo 2 decimales. |
| `length`                              | `decimal`                   | Sí          | Largo (metros). Debe ser mayor a cero. Máximo 2 decimales. |
| `maximum_number_of_pallets_per_level` | `integer`                   | Condicional | Solo aplica con `Aisle` + `Pallets`: obligatorio y mayor a cero. No debe enviarse con `None` ni en secciones `Storage`. |

### Combinaciones válidas

| `section_type` | `section_storage_type` | Código generado | Notas |
|----------------|------------------------|-----------------|-------|
| `Storage`      | `Racks`                | `SR-xx`         | Sección de racks. |
| `Storage`      | `Lots`                 | `ST-xx`         | Sección de tramos. |
| `Aisle`        | `Pallets`              | `SP-xx`         | Pasillo con almacenamiento; requiere `maximum_number_of_pallets_per_level`. |
| `Aisle`        | `None`                 | `SP-xx`         | Pasillo sin almacenamiento; no enviar max polines. |

Ejemplo sección de almacenamiento (racks):

```json
{
  "section_type": "Storage",
  "section_storage_type": "Racks",
  "width": 20.00,
  "length": 25.00
}
```

Ejemplo sección de tramos:

```json
{
  "section_type": "Storage",
  "section_storage_type": "Lots",
  "width": 20.00,
  "length": 25.00
}
```

Ejemplo pasillo con polines:

```json
{
  "section_type": "Aisle",
  "section_storage_type": "Pallets",
  "width": 3.50,
  "length": 40.00,
  "maximum_number_of_pallets_per_level": 2
}
```

Ejemplo pasillo sin almacenamiento:

```json
{
  "section_type": "Aisle",
  "section_storage_type": "None",
  "width": 3.50,
  "length": 40.00
}
```

---

## Respuestas

### ✅ 201 Created

El recurso se creó correctamente. El cuerpo de la respuesta puede ir vacío.

### Notas

| Campo / regla | Descripción |
|---|---|
| Código automático | Generado con `ICodeGenerator.GenerateUniqueSectionCodeAsync`. Prefijos: `SR` (racks), `ST` (tramos), `SP` (pasillos). Secuencia por almacén con 2 dígitos. |
| Capacidad de sección | Se calcula con `width`, `length` y `section_type` y se persiste en `section_capacity`. |
| Capacidad de almacén | El almacén **debe** tener `warehouse_capacity`. Se actualiza tras el cálculo. |
| Rol | Solo `Administrator`. Otros roles: `No tienes permiso para realizar esta acción`. |
| Almacén inválido | `El almacén indicado no existe o no está activo.` |
| Storage inválido | `Una sección de almacenamiento solo admite Racks o Lots.` (`ERP:SECTION_STORAGE_MISMATCH`) |
| Aisle inválido | `Una sección de tipo pasillo solo admite almacenamiento en polines o sin almacenamiento.` (`ERP:SECTION_STORAGE_MISMATCH`) |
| Max polines fuera de Aisle | `El número máximo de polines por nivel solo es para tipo pasillo (Aisle).` |
| Aisle + Pallets sin max | `Si el pasillo permite almacenamiento (Polines), el número máximo de polines por nivel debe ser mayor a cero.` |
| Aisle + None con max | `El número máximo de polines por nivel solo aplica cuando el pasillo permite almacenamiento (Pallets).` |
| Fallo al generar código | `No se pudo generar el código de la sección. Verifica el tipo de sección y el tipo de almacenamiento.` (`ERP:SECTION_CODE_GENERATION_FAILED`) |
| Cálculo fallido | `No se pudo calcular la capacidad de la sección.` |
| Sin capacidad de almacén | `El almacén no tiene capacidad registrada` |

### ❌ 400 Bad Request

```json
{
  "status": 400,
  "error": {
    "type_error": "ValidationError",
    "description": "El usuario no tiene acceso a esta compañía o módulo"
  },
  "created_at": "2026-09-28 12:00:00"
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
  "created_at": "2026-09-28 12:00:00"
}
```

---

## Códigos de Estado

| Código | Descripción |
|---|---|
| `201` | Sección registrada exitosamente. |
| `400` | Error de validación, acceso, permisos o reglas de negocio (`ErrorResponse`). |
| `500` | Error interno del servidor (`ErrorResponse`). |
