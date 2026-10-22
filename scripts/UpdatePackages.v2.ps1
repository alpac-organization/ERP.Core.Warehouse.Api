# UpdatePackages.v2.ps1
# Obtiene la ultima version estable de los paquetes ERP.Core desde GitHub Packages
# y actualiza los mismos 7 paquetes sin necesidad de pasar la version por parametro.

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Success($msg) { Write-Host "[OK]   $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[ERR]  $msg" -ForegroundColor Red }

try {
    # 1) Detectar raiz del repo actual (funciona sin importar cwd)
    $repoRoot = $null
    try {
        $repoRoot = git -C $PSScriptRoot rev-parse --show-toplevel 2>$null
    }
    catch { }

    if (-not $repoRoot -or -not (Test-Path $repoRoot)) {
        $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..") | Select-Object -ExpandProperty Path
    }

    $repoRoot = $repoRoot.Trim()
    Write-Info "Repo root: $repoRoot"

    # Validar que es Warehouse.Api
    $sln = Join-Path $repoRoot "ERP.Core.Warehouse.Api.slnx"
    if (-not (Test-Path $sln)) {
        throw "No se encontro ERP.Core.Warehouse.Api.slnx en la raiz del repo. Ejecuta este script desde el repo ERP.Core.Warehouse.Api."
    }

    # 2) Leer nuget.config para obtener credenciales de GitHub Packages
    $nugetConfig = Join-Path $repoRoot "nuget.config"
    if (-not (Test-Path $nugetConfig)) {
        throw "No se encontro nuget.config en $repoRoot. Se necesita para autenticarse contra GitHub Packages."
    }

    Write-Info "Leyendo credenciales desde nuget.config..."
    [xml]$xml = Get-Content $nugetConfig -Raw

    $username = $null
    $token = $null

    $githubCreds = $xml.configuration.packageSourceCredentials.GitHub
    if ($githubCreds) {
        foreach ($node in $githubCreds.add) {
            if ($node.key -eq "Username") { $username = $node.value }
            if ($node.key -eq "ClearTextPassword") { $token = $node.value }
        }
    }

    if (-not $username -or -not $token) {
        throw "No se encontraron Username/ClearTextPassword para la fuente GitHub en nuget.config."
    }

    # 3) Consultar ultima version estable desde GitHub Packages
    $packageId = "ERP.Core.Application"
    $org = "alpac-organization"
    $downloadUrl = "https://nuget.pkg.github.com/$org/download/$packageId/index.json"

    Write-Info "Consultando ultima version de $packageId desde GitHub Packages..."
    Write-Info "Endpoint: $downloadUrl"

    $pair = "$($username):$($token)"
    $bytes = [System.Text.Encoding]::ASCII.GetBytes($pair)
    $basic = [Convert]::ToBase64String($bytes)
    $headers = @{
        "Authorization" = "Basic $basic"
        "User-Agent"    = "UpdatePackages.v2"
        "Accept"        = "application/json"
    }

    try {
        $response = Invoke-RestMethod -Uri $downloadUrl -Headers $headers -Method Get -ErrorAction Stop
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq 401 -or $status -eq 403) {
            Write-Err "Autenticacion fallida contra GitHub Packages ($status)."
            Write-Err "Revisa el token (ClearTextPassword) en nuget.config. Puede estar caducado/revocado o sin permisos."
            Write-Err "Feed/paquetes: https://github.com/orgs/$org/packages"
            throw
        }
        throw
    }

    $versions = @($response.versions)
    if ($versions.Count -eq 0) {
        throw "No se encontraron versiones para $packageId. Verifica que el paquete exista y que el token tenga acceso."
    }

    # Filtrar solo estables (sin prerelease) y quedarse con la mayor por semver
    $stable = $versions | Where-Object { $_ -notmatch '-' } | Sort-Object { [version]$_ } -Descending
    if (-not $stable -or $stable.Count -eq 0) {
        Write-Warn "No hay versiones estables, usando la ultima disponible (incluyendo prerelease)."
        $latest = $versions | Sort-Object { [version]($_ -replace '-.*$', '') } -Descending | Select-Object -First 1
    }
    else {
        $latest = $stable | Select-Object -First 1
    }

    Write-Success "Ultima version estable detectada: $latest"

    # 4) Actualizar los 7 paquetes
    $packages = @(
        @{ Project = (Join-Path $repoRoot "src/Application/Application.csproj"); Name = "ERP.Core.Application" },
        @{ Project = (Join-Path $repoRoot "src/Application/Application.csproj"); Name = "ERP.Core.Database.Application" },
        @{ Project = (Join-Path $repoRoot "src/Domain/Domain.csproj"); Name = "ERP.Core.Domain" },
        @{ Project = (Join-Path $repoRoot "src/Domain/Domain.csproj"); Name = "ERP.Core.Database.Domain" },
        @{ Project = (Join-Path $repoRoot "src/Infrastructure/Infrastructure.csproj"); Name = "ERP.Core.Infrastructure" },
        @{ Project = (Join-Path $repoRoot "src/Infrastructure/Infrastructure.csproj"); Name = "ERP.Core.Database.Infrastructure" },
        @{ Project = (Join-Path $repoRoot "test/ERP.Core.Warehouse.Api.Test/ERP.Core.Warehouse.Api.Test.csproj"); Name = "ERP.Core.Testing" }
    )

    Write-Info "Actualizando paquetes..."
    foreach ($pkg in $packages) {
        if (-not (Test-Path $pkg.Project)) {
            Write-Warn "No se encontro: $($pkg.Project) - omitiendo"
            continue
        }
        Write-Host "  -> $($pkg.Name) -> $latest [$($pkg.Project)]" -ForegroundColor Cyan
        & dotnet add $pkg.Project package $pkg.Name --version $latest
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet add fallo para $($pkg.Name)"
        }
    }

    Write-Success "Listo. Paquetes actualizados a $latest"
}
catch {
    Write-Err $_.Exception.Message
    exit 1
}
