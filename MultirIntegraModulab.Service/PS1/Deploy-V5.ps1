# DEPLOYMENT SCRIPT - COPIAR TOTS ELS FITXERS NECESSARIS

Write-Host "=== DEPLOYMENT QUARTZ V5 ===" -ForegroundColor Cyan
Write-Host ""

$source = "C:\Projectes\MultirIntegraModulab\MultirIntegraModulab.Service\bin\Release\"
$dest = "C:\MultiR\"

if (-not (Test-Path $source)) {
	Write-Host "ERROR: Directori source no existeix: $source" -ForegroundColor Red
	exit
}

# 1. Aturar servei
Write-Host "1. Aturant servei..." -ForegroundColor Yellow
Stop-Service MultirIntegraModulabService -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# 2. Copiar executable
Write-Host "2. Copiant executable..." -ForegroundColor Yellow
$exeFile = Join-Path $source "MultirIntegraModulab.Service.exe"
Copy-Item $exeFile $dest -Force
Write-Host "   ✅ Copiat: MultirIntegraModulab.Service.exe" -ForegroundColor Green

# 3. Copiar .config
Write-Host "3. Copiant configuració..." -ForegroundColor Yellow
$configFile = Join-Path $source "MultirIntegraModulab.Service.exe.config"
Copy-Item $configFile $dest -Force
Write-Host "   ✅ Copiat: MultirIntegraModulab.Service.exe.config" -ForegroundColor Green

# 4. Copiar DLLs crítiques (per si de cas)
Write-Host "4. Copiant DLLs..." -ForegroundColor Yellow
$dlls = @(
	"Quartz.dll",
	"Newtonsoft.Json.dll",
	"Microsoft.Extensions.Logging.Abstractions.dll"
)

foreach ($dll in $dlls) {
	$dllPath = Join-Path $source $dll
	if (Test-Path $dllPath) {
		Copy-Item $dllPath $dest -Force
		Write-Host "   ✅ Copiat: $dll" -ForegroundColor Green
	}
}

# 5. Copiar workflow-schedule.json si existeix
Write-Host "5. Copiant configuració de workflows..." -ForegroundColor Yellow
$jsonFile = Join-Path $source "workflow-schedule.json"
if (Test-Path $jsonFile) {
	Copy-Item $jsonFile $dest -Force
	Write-Host "   ✅ Copiat: workflow-schedule.json" -ForegroundColor Green
} else {
	Write-Host "   ⚠️  Avís: workflow-schedule.json no trobat a $source" -ForegroundColor Yellow
	Write-Host "      (Usant el que ja existeix a $dest)" -ForegroundColor Yellow
}

# 6. Verificar que tots els fitxers estan
Write-Host ""
Write-Host "6. VERIFICACIÓ DE FITXERS AL SERVIDOR:" -ForegroundColor Cyan

$requiredFiles = @(
	"MultirIntegraModulab.Service.exe",
	"MultirIntegraModulab.Service.exe.config",
	"Quartz.dll",
	"Newtonsoft.Json.dll",
	"Microsoft.Extensions.Logging.Abstractions.dll"
)

$allPresent = $true
foreach ($file in $requiredFiles) {
	$filePath = Join-Path $dest $file
	if (Test-Path $filePath) {
		$item = Get-Item $filePath
		Write-Host "   ✅ $file ($('{0:dd/MM/yyyy HH:mm:ss}' -f $item.LastWriteTime))" -ForegroundColor Green
	} else {
		Write-Host "   ❌ $file NO TROVATO!" -ForegroundColor Red
		$allPresent = $false
	}
}

if (-not $allPresent) {
	Write-Host ""
	Write-Host "ERROR: Falten fitxers crítics!" -ForegroundColor Red
	exit
}

# 7. Iniciar servei
Write-Host ""
Write-Host "7. Iniciant servei..." -ForegroundColor Yellow
Start-Service MultirIntegraModulabService

Write-Host "8. Esperant que s'iniciï..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# 8. Veure logs d'inici
Write-Host ""
Write-Host "9. LOGS D'INICI:" -ForegroundColor Green

$logs = Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 10

$errors = $logs | Where-Object {$_.EntryType -eq 'Error'}
if ($errors) {
	Write-Host "   ❌ ERRORS:" -ForegroundColor Red
	$errors | ForEach-Object { Write-Host "      $($_.Message.Substring(0, 100))" -ForegroundColor Red }
} else {
	Write-Host "   ✅ Cap errors" -ForegroundColor Green
}

$schedulerOK = $logs | Where-Object {$_.Message -like "*Scheduler Quartz iniciat*"}
if ($schedulerOK) {
	Write-Host "   ✅ Scheduler iniciat correctament" -ForegroundColor Green
} else {
	Write-Host "   ❌ Scheduler no iniciat!" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== DEPLOYMENT COMPLETAT ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pots executar el test amb: .\Test-QuartzV4-Final.ps1" -ForegroundColor Yellow
