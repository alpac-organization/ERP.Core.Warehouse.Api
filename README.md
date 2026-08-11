# ERP.Core.Warehouse.Api

API del dominio de **Bodegas (Warehouse)** dentro del ERP Core. Expone los endpoints para la gestión de inventario, ubicaciones y operaciones relacionadas con almacenes.

## 🧱 Stack técnico

- **.NET** (ASP.NET Core Web API)
- **Swagger / OpenAPI** para documentación interactiva
- Arquitectura por capas:
  - `Domain` — acceso a datos, servicios externos, configuración
  - `Application` — casos de uso / lógica de aplicación
  - `Infrastructure` — acceso a datos, servicios externos, configuración
  - `ERP.Core.Warehouse.Api` - capa de presentación, acceso a controladores y manejo de documentación

## 📋 Requisitos previos

- [.NET SDK `10`](https://dotnet.microsoft.com/download)
- Un archivo `.env` en la raíz del repositorio (o del proyecto) con las variables necesarias

## ⚙️ Configuración

### Variables de entorno

El proyecto busca un archivo `.env` en la raíz del repo (dos niveles arriba del proyecto). Si no lo encuentra ahí, intenta cargarlo desde el directorio de trabajo actual.

Crea un archivo `.env` con al menos estas variables (ajustá según tu configuración real):

> ⚠️ Completa esta sección con las variables reales que consume tu `IConfiguration` en `AddInfrastructureServices` y en los middlewares de Auth/ApiKey.

### CORS

Los orígenes permitidos se leen desde la sección `Cors:AllowedOrigins` de la configuración (típicamente `appsettings.json` o variables de entorno). Por defecto está pensado para consumo desde un frontend Vite en desarrollo local.

## 🚀 Cómo correr el proyecto

```bash
# Restaurar dependencias
dotnet restore

# Correr en modo desarrollo
dotnet run --project ERP.Core.Warehouse.Api

# O con hot reload
dotnet watch run --project ERP.Core.Warehouse.Api
```

La API quedará disponible en `https://localhost:7217` (ver `launchSettings.json` o `.env` para el puerto configurado).

## 📖 Documentación de la API (Swagger)

En entorno de **desarrollo**, la UI de Swagger está disponible en:

```
https://localhost:7217/swagger/docs
```

> El JSON de OpenAPI se sirve en `/swagger/v1/swagger.json`.

## 🔐 Seguridad

La API utiliza los siguientes middlewares, en este orden:

1. `ExceptionMiddleware` — captura y formatea errores no controlados
2. `ApiKeyMiddleware` — valida la API key en las cabeceras del request
3. `AuthMiddleware` — valida autenticación/autorización del usuario

Asegurate de incluir los headers correspondientes en tus requests:

```
X-Api-Key: <tu_api_key>
Authorization: Bearer <tu_token>
```

## 📂 Estructura del proyecto

```
ERP.Core.Warehouse.Api/
├── Domain /               # Capa de dominio, entidades y modelo de negocio
├── Application/           # Casos de uso, DTOs, lógica de negocio
├── Infrastructure/        # Repositorios, servicios externos, configuración
├── ERP.Core.Warehouse.Api # Controladores y documentación
```

## 🧪 Testing

```bash
dotnet test
```

> Completa esta sección si tenés proyectos de test asociados (unitarios, integración).

## 📝 Notas adicionales

- El serializador JSON usa **snake_case** para las propiedades y convierte los `enum` a string automáticamente.
- Los logs de licencia de `LuckyPennySoftware.MediatE` están silenciados por configuración.

---

`<Agregá aquí info de contacto, convenciones de branching, CI/CD, o cualquier otra nota relevante para tu equipo>`