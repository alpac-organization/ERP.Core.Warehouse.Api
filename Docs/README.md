# Documentación de endpoints — ERP.Core.Warehouse.Api

Este directorio contiene la documentación de los **endpoints** del backend de almacén.

El sitio de documentación (Astro Starlight, publicado en GitHub Pages) lee **solo los
archivos Markdown que existen aquí** y los publica automáticamente.

## Cómo agregar documentación

Añade un archivo Markdown por endpoint, con la estructura `<Módulo>/<Endpoint>Docs.md`:

```
Docs/
└── <Módulo>/<Endpoint>Docs.md
```

Por ejemplo:

```
Docs/Products/RegisterProductDocs.md
Docs/Movements/CreateMovementDocs.md
```

El archivo se publica en el sitio como página:
`/api/almacen/<modulo>/<endpoint>/`.

## Formato recomendado

No requiere frontmatter (el título se deduce de la cabecera). Un ejemplo:

```md
# Almacén

## Registrar Producto

Endpoint para registrar un producto en el almacén.

## Información General

| Campo | Valor |
|-------|-------|
| **Método**      | `POST` |
| **Endpoint**    | `/api/v1/companies/{company_id}/modules/{module_code}/products` |
| **Descripción** | Registra un nuevo producto. |
```

Puedes incluir secciones de **Parámetros**, **Headers**, **Request Body** y **Respuestas**.

> Los archivos `README*.md` se ignoran: NO se publican como endpoints.
