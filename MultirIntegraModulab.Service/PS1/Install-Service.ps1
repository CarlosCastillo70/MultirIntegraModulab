# Script d'instal·lació del Windows Service MultirIntegraModulab
# Requereix executar com a Administrador

param(
	[string]$ServicePath = $null
)

# Verificar privilegis d'administrador
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
	Write-Error "Aquest script requereix privilegis d'administrador. Executeu PowerShell com a Administrador."
	exit 1
}

$ServiceName = "MultirIntegraModulabService"
$ServiceDisplayName = "MultiR Integra Modulab Service"
$ServiceDescription = "Servei per executar periòdicament la integració de mostres Modulab i la revisió de vigència de diagnòstics"

# Si no s'especifica el path, usar el directori actual
if ([string]::IsNullOrEmpty($ServicePath))
{
	$ServicePath = Join-Path $PSScriptRoot "MultirIntegraModulab.Service.exe"
}

# Verificar que l'executable existeix
if (-not (Test-Path $ServicePath))
{
	Write-Error "No s'ha trobat l'executable del servei: $ServicePath"
	exit 1
}

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Instal·lació del Windows Service" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Verificar si el servei ja existeix
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService)
{
	Write-Host "⚠️  El servei ja existeix. Aturant i desinstal·lant..." -ForegroundColor Yellow

	if ($existingService.Status -eq 'Running')
	{
		Stop-Service -Name $ServiceName -Force
		Start-Sleep -Seconds 2
	}

	# Desinstal·lar servei existent
	sc.exe delete $ServiceName | Out-Null
	Start-Sleep -Seconds 2
}

Write-Host "📦 Instal·lant servei..." -ForegroundColor Green
Write-Host "   Nom: $ServiceName"
Write-Host "   Executable: $ServicePath"
Write-Host ""

# Crear el servei
$result = sc.exe create $ServiceName binPath= "`"$ServicePath`"" start= auto DisplayName= "$ServiceDisplayName"

if ($LASTEXITCODE -ne 0)
{
	Write-Error "Error creant el servei. Codi d'error: $LASTEXITCODE"
	exit 1
}

# Configurar descripció
sc.exe description $ServiceName "$ServiceDescription" | Out-Null

# Configurar recuperació automàtica en cas de fallada
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null

Write-Host "✅ Servei instal·lat correctament!" -ForegroundColor Green
Write-Host ""

# Crear Event Log Sources
Write-Host "📝 Creant Event Log Sources..." -ForegroundColor Green

if (-not [System.Diagnostics.EventLog]::SourceExists("MultirIntegraModulabService"))
{
    [System.Diagnostics.EventLog]::CreateEventSource("MultirIntegraModulabService", "Application")
    Write-Host "   ✓ Event Source 'MultirIntegraModulabService' creat" -ForegroundColor Gray
}
else
{
    Write-Host "   ✓ Event Source 'MultirIntegraModulabService' ja existeix" -ForegroundColor Gray
}

if (-not [System.Diagnostics.EventLog]::SourceExists("MultirRevisioVigenciaService"))
{
    [System.Diagnostics.EventLog]::CreateEventSource("MultirRevisioVigenciaService", "Application")
    Write-Host "   ✓ Event Source 'MultirRevisioVigenciaService' creat" -ForegroundColor Gray
}
else
{
    Write-Host "   ✓ Event Source 'MultirRevisioVigenciaService' ja existeix" -ForegroundColor Gray
}

Write-Host "✅ Event Log Sources configurats!" -ForegroundColor Green
Write-Host ""

# Preguntar si vol iniciar el servei
$startService = Read-Host "Voleu iniciar el servei ara? (S/N)"

if ($startService -eq 'S' -or $startService -eq 's')
{
	Write-Host "🚀 Iniciant servei..." -ForegroundColor Green
	Start-Service -Name $ServiceName
	Start-Sleep -Seconds 2

	$service = Get-Service -Name $ServiceName
	if ($service.Status -eq 'Running')
	{
		Write-Host "✅ Servei iniciat correctament!" -ForegroundColor Green
	}
	else
	{
		Write-Host "⚠️  El servei no s'ha pogut iniciar. Reviseu els logs d'esdeveniments." -ForegroundColor Yellow
	}
}

Write-Host ""
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host " Tasques programades:" -ForegroundColor Cyan
Write-Host "===================================================================" -ForegroundColor Cyan
Write-Host "  - Processar mostres Modulab: cada 15 minuts"
Write-Host "  - Revisar vigencia diagnostics: cada dia a les 4:00 AM"
Write-Host ""
Write-Host "Per veure els logs: Event Viewer - Application" -ForegroundColor Cyan
Write-Host "  Origen: MultirIntegraModulabService"
Write-Host "  Origen: MultirRevisioVigenciaService"
Write-Host ""
