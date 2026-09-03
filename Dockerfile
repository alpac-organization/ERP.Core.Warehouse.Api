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

ENV XDG_DATA_HOME=/app/data
RUN mkdir -p /app/data/Puppeteer /tmp/warmup \
    && (cd /tmp/warmup \
        && dotnet new console --force \
        && dotnet add package PuppeteerSharp --version 25.8.0 \
        && printf 'using PuppeteerSharp;\nvar fetcher = new BrowserFetcher();\nawait fetcher.DownloadAsync();\n' > Program.cs \
        && dotnet run --configuration Release) \
        || echo "[warmup] No se pudo pre-descargar Chromium; se descargará en runtime."

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends wget ca-certificates fonts-liberation \
    && wget -q "https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb" -O /tmp/chrome.deb \
    && apt-get install -y /tmp/chrome.deb \
    && rm -f /tmp/chrome.deb \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/google-chrome
ENV XDG_DATA_HOME=/app/data

COPY --from=build /app/data/Puppeteer /app/data/Puppeteer

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

EXPOSE 8080

ENTRYPOINT ["dotnet", "ERP.Core.Warehouse.Api.dll"]