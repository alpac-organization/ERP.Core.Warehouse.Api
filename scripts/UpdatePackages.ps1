# update-erp-packages.ps1
param(
   [Parameter(Mandatory = $true)]
   [string]$Version
)

$ErrorActionPreference = "Stop"

$packages = @(
   @{ Project = "../src/Application/Application.csproj"; Name = "ERP.Core.Application" },
   @{ Project = "../src/Application/Application.csproj"; Name = "ERP.Core.Database.Application" },
   @{ Project = "../src/Domain/Domain.csproj"; Name = "ERP.Core.Domain" },
   @{ Project = "../src/Domain/Domain.csproj"; Name = "ERP.Core.Database.Domain" },
   @{ Project = "../src/Infrastructure/Infrastructure.csproj"; Name = "ERP.Core.Infrastructure" },
   @{ Project = "../src/Infrastructure/Infrastructure.csproj"; Name = "ERP.Core.Database.Infrastructure" }
   @{ Project = "../test/ERP.Core.Warehouse.Api.Test/ERP.Core.Warehouse.Api.Test.csproj"; Name = "ERP.Core.Testing" }
)

foreach ($pkg in $packages) {
   Write-Host "Updating $($pkg.Name) -> $Version in $($pkg.Project)" -ForegroundColor Cyan
   dotnet add $pkg.Project package $pkg.Name --version $Version
}

Write-Host "Done." -ForegroundColor Green