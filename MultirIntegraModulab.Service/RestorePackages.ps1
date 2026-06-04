# Script per restaurar paquets NuGet del projecte MultirIntegraModulab.Service
# Executa aquest script des del directori del projecte

Write-Host "Descarregant paquets NuGet..." -ForegroundColor Cyan

# Crear directori packages si no existeix
$packagesDir = "..\packages"
if (-not (Test-Path $packagesDir)) {
	New-Item -ItemType Directory -Path $packagesDir | Out-Null
}

# URL base de NuGet
$nugetApiUrl = "https://api.nuget.org/v3-flatcontainer"

# Funcio per descarregar un paquet NuGet
function Download-NuGetPackage {
	param(
		[string]$PackageId,
		[string]$Version,
		[string]$TargetFramework
	)

	$packageName = "$PackageId.$Version"
	$packageDir = Join-Path $packagesDir $packageName

	if (Test-Path $packageDir) {
		Write-Host "  OK $PackageId $Version ja existeix" -ForegroundColor Green
		return
	}

	Write-Host "  -> Descarregant $PackageId $Version..." -ForegroundColor Yellow

	try {
		$packageUrl = "$nugetApiUrl/$($PackageId.ToLower())/$Version/$($PackageId.ToLower()).$Version.nupkg"
		$nupkgPath = Join-Path $packagesDir "$packageName.nupkg"

		# Descarregar el paquet
		Invoke-WebRequest -Uri $packageUrl -OutFile $nupkgPath -UseBasicParsing

		# Extreure el paquet (els .nupkg son fitxers ZIP)
		Expand-Archive -Path $nupkgPath -DestinationPath $packageDir -Force

		# Eliminar el .nupkg despres d'extreure
		Remove-Item $nupkgPath -Force

		Write-Host "  OK $PackageId $Version descarregat correctament" -ForegroundColor Green
	}
	catch {
		Write-Host "  ERROR descarregant $PackageId $Version : $_" -ForegroundColor Red
	}
}

# Descarregar paquets necessaris
Download-NuGetPackage -PackageId "Quartz" -Version "3.6.2" -TargetFramework "net48"
Download-NuGetPackage -PackageId "Newtonsoft.Json" -Version "13.0.3" -TargetFramework "net48"
Download-NuGetPackage -PackageId "Microsoft.Extensions.Logging.Abstractions" -Version "2.1.1" -TargetFramework "net48"

Write-Host ""
Write-Host "Paquets restaurats correctament!" -ForegroundColor Green
Write-Host "Ara pots compilar el projecte des de Visual Studio." -ForegroundColor Cyan
