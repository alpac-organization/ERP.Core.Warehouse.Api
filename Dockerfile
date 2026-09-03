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

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libatspi2.0-0 \
        libcups2 libdrm2 libxkbcommon0 libxkbcommon-x11-0 libxcomposite1 \
        libxdamage1 libxfixes3 libxrandr2 libgbm1 libasound2 \
        libpangocairo-1.0-0 libpango-1.0-0 libcairo2 libglib2.0-0 \
        libx11-6 libxcb1 libxext6 libxcursor1 libxi6 libxss1 libxtst6 \
        libgtk-3-0 fonts-liberation ca-certificates wget procps \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "ERP.Core.Warehouse.Api.dll"]