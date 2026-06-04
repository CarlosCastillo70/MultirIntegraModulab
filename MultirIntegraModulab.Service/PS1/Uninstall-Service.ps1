# Script de desinstal·lació del Windows Service MultirIntegraModulab
# Requereix executar com a Administrador

# Verificar privilegis d'administrador
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
	Write-Error "Aquest script requereix privilegis d'administrador. Executeu PowerShell com a Administrador."
	exit 1
}

$ServiceName = "MultirIntegraModulabService"

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Desinstal·lació del Windows Service" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar si el servei existeix
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if (-not $service)
{
	Write-Host "ℹ️  El servei no està instal·lat." -ForegroundColor Yellow
	exit 0
}

Write-Host "🛑 Aturant servei..." -ForegroundColor Yellow

# Aturar el servei si està en execució
if ($service.Status -eq 'Running')
{
	Stop-Service -Name $ServiceName -Force
	Start-Sleep -Seconds 2
	Write-Host "   Servei aturat correctament" -ForegroundColor Green
}

Write-Host "🗑️  Desinstal·lant servei..." -ForegroundColor Yellow

# Eliminar el servei
sc.exe delete $ServiceName | Out-Null

if ($LASTEXITCODE -eq 0)
{
	Write-Host "✅ Servei desinstal·lat correctament!" -ForegroundColor Green
}
else
{
	Write-Error "Error desinstal·lant el servei. Codi d'error: $LASTEXITCODE"
	exit 1
}

Write-Host ""
Write-Host "ℹ️  Els logs del servei es mantenen al Visor d'Esdeveniments" -ForegroundColor Cyan
Write-Host ""
