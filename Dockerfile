FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

ARG GH_PACKAGE_TOKEN
ARG GH_USER

# GitHub Packages
RUN dotnet nuget add source "https://nuget.pkg.github.com/alpac-organization/index.json" \
    --name "GitHub" \
    --username "$GH_USER" \
    --password "$GH_PACKAGE_TOKEN" \
    --store-password-in-clear-text

# Copiar archivos de proyecto
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/ERP.Core.Warehouse.Api/ERP.Core.Warehouse.Api.csproj", "src/ERP.Core.Warehouse.Api/"]

# Restaurar dependencias
RUN dotnet restore "src/ERP.Core.Warehouse.Api/ERP.Core.Warehouse.Api.csproj"

# Copiar el código fuente
COPY src/ ./src/

# Ir al proyecto principal
WORKDIR "/src/src/ERP.Core.Warehouse.Api"

# Publicar la aplicación (compila automáticamente)
RUN dotnet publish "ERP.Core.Warehouse.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# 2. RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "ERP.Core.Warehouse.Api.dll"]