# Script de Verificació de Build i Fitxers de Configuració
# Verifica que tots els executables i els seus .config existeixen després de compilar

param(
	[string]$Configuration = "Release"
)

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Verificació de Build - MultirIntegraModulab" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$errors = @()
$warnings = @()

# ──────────────────────────────────────────────────────────────
# 1. VERIFICAR MULTIRINTEGRAMODULAB
# ──────────────────────────────────────────────────────────────

Write-Host "📦 Verificant MultirIntegraModulab..." -ForegroundColor Green

$modulabPath = "MultirIntegraModulab\bin\$Configuration"
$modulabExe = Join-Path $modulabPath "MultirIntegraModulab.exe"
$modulabConfig = Join-Path $modulabPath "MultirIntegraModulab.exe.config"

if (Test-Path $modulabExe) {
	Write-Host "   ✓ MultirIntegraModulab.exe trobat" -ForegroundColor Gray
} else {
	$errors += "❌ No s'ha trobat MultirIntegraModulab.exe a $modulabPath"
}

if (Test-Path $modulabConfig) {
	Write-Host "   ✓ MultirIntegraModulab.exe.config trobat" -ForegroundColor Gray

	# Verificar contingut del config
	$configContent = Get-Content $modulabConfig -Raw
	if ($configContent -match '<add key="Entorn"') {
		Write-Host "   ✓ Configuració 'Entorn' present" -ForegroundColor Gray

		if ($configContent -match 'value="Produccio"') {
			Write-Host "   ⚠️  Entorn configurat com PRODUCCIÓ" -ForegroundColor Yellow
			$warnings += "MultirIntegraModulab.exe.config està en mode PRODUCCIÓ"
		} elseif ($configContent -match 'value="Preproduccio"') {
			Write-Host "   ℹ️  Entorn configurat com PREPRODUCCIÓ" -ForegroundColor Cyan
		}
	}
} else {
	$errors += "❌ No s'ha trobat MultirIntegraModulab.exe.config a $modulabPath"
}

Write-Host ""

# ──────────────────────────────────────────────────────────────
# 2. VERIFICAR MULTIRREVIIOVIGENCIA
# ──────────────────────────────────────────────────────────────

Write-Host "📦 Verificant MultirRevisioVigencia..." -ForegroundColor Green

$vigenciaPath = "MultirRevisioVigencia\bin\$Configuration"
$vigenciaExe = Join-Path $vigenciaPath "MultirRevisioVigencia.exe"
$vigenciaConfig = Join-Path $vigenciaPath "MultirRevisioVigencia.exe.config"

if (Test-Path $vigenciaExe) {
	Write-Host "   ✓ MultirRevisioVigencia.exe trobat" -ForegroundColor Gray
} else {
	$errors += "❌ No s'ha trobat MultirRevisioVigencia.exe a $vigenciaPath"
}

if (Test-Path $vigenciaConfig) {
	Write-Host "   ✓ MultirRevisioVigencia.exe.config trobat" -ForegroundColor Gray

	# Verificar contingut del config
	$configContent = Get-Content $vigenciaConfig -Raw
	if ($configContent -match '<add key="Entorn"') {
		Write-Host "   ✓ Configuració 'Entorn' present" -ForegroundColor Gray

		if ($configContent -match 'value="Produccio"') {
			Write-Host "   ⚠️  Entorn configurat com PRODUCCIÓ" -ForegroundColor Yellow
			$warnings += "MultirRevisioVigencia.exe.config està en mode PRODUCCIÓ"
		} elseif ($configContent -match 'value="Preproduccio"') {
			Write-Host "   ℹ️  Entorn configurat com PREPRODUCCIÓ" -ForegroundColor Cyan
		}
	}
} else {
	$errors += "❌ No s'ha trobat MultirRevisioVigencia.exe.config a $vigenciaPath"
}

Write-Host ""

# ──────────────────────────────────────────────────────────────
# 3. VERIFICAR WINDOWS SERVICE
# ──────────────────────────────────────────────────────────────

Write-Host "📦 Verificant MultirIntegraModulab.Service..." -ForegroundColor Green

$servicePath = "MultirIntegraModulab.Service\bin\$Configuration"
$serviceExe = Join-Path $servicePath "MultirIntegraModulab.Service.exe"
$serviceConfig = Join-Path $servicePath "MultirIntegraModulab.Service.exe.config"
$workflowSchedule = Join-Path $servicePath "workflow-schedule.json"

if (Test-Path $serviceExe) {
	Write-Host "   ✓ MultirIntegraModulab.Service.exe trobat" -ForegroundColor Gray
} else {
	$errors += "❌ No s'ha trobat MultirIntegraModulab.Service.exe a $servicePath"
}

if (Test-Path $serviceConfig) {
	Write-Host "   ✓ MultirIntegraModulab.Service.exe.config trobat" -ForegroundColor Gray
} else {
	$errors += "❌ No s'ha trobat MultirIntegraModulab.Service.exe.config a $servicePath"
}

if (Test-Path $workflowSchedule) {
	Write-Host "   ✓ workflow-schedule.json trobat" -ForegroundColor Gray

	# Verificar que és JSON vàlid
	try {
		$json = Get-Content $workflowSchedule -Raw | ConvertFrom-Json
		Write-Host "   ✓ workflow-schedule.json és JSON vàlid" -ForegroundColor Gray
		Write-Host "   ℹ️  Tasques programades: $($json.Count)" -ForegroundColor Cyan
	} catch {
		$errors += "❌ workflow-schedule.json no és un JSON vàlid: $($_.Exception.Message)"
	}
} else {
	$errors += "❌ No s'ha trobat workflow-schedule.json a $servicePath"
}

Write-Host ""

# ──────────────────────────────────────────────────────────────
# 4. VERIFICAR DEPENDÈNCIES
# ──────────────────────────────────────────────────────────────

Write-Host "📦 Verificant dependències..." -ForegroundColor Green

$dependencies = @(
	@{ Name = "Quartz.dll"; Path = $servicePath },
	@{ Name = "Newtonsoft.Json.dll"; Path = $servicePath },
	@{ Name = "MySql.Data.dll"; Path = $servicePath }
)

foreach ($dep in $dependencies) {
	$depPath = Join-Path $dep.Path $dep.Name
	if (Test-Path $depPath) {
		Write-Host "   ✓ $($dep.Name) trobat" -ForegroundColor Gray
	} else {
		$warnings += "⚠️  No s'ha trobat $($dep.Name) a $($dep.Path)"
	}
}

Write-Host ""

# ──────────────────────────────────────────────────────────────
# 5. RESUM
# ──────────────────────────────────────────────────────────────

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Resum de Verificació" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($errors.Count -eq 0 -and $warnings.Count -eq 0) {
	Write-Host "✅ TOTES LES VERIFICACIONS HAN PASSAT CORRECTAMENT" -ForegroundColor Green
	Write-Host ""
	Write-Host "Tots els executables i els seus fitxers de configuració estan presents." -ForegroundColor Gray
	Write-Host ""

	# Mostrar paths per copiar
	Write-Host "📁 Paths per copiar al servidor:" -ForegroundColor Cyan
	Write-Host "   MultirIntegraModulab: $modulabPath" -ForegroundColor Gray
	Write-Host "   MultirRevisioVigencia: $vigenciaPath" -ForegroundColor Gray
	Write-Host "   Windows Service: $servicePath" -ForegroundColor Gray
	Write-Host ""

	exit 0
}

if ($errors.Count -gt 0) {
	Write-Host "❌ ERRORS TROBATS:" -ForegroundColor Red
	foreach ($error in $errors) {
		Write-Host "   $error" -ForegroundColor Red
	}
	Write-Host ""
}

if ($warnings.Count -gt 0) {
	Write-Host "⚠️  WARNINGS:" -ForegroundColor Yellow
	foreach ($warning in $warnings) {
		Write-Host "   $warning" -ForegroundColor Yellow
	}
	Write-Host ""
}

if ($errors.Count -gt 0) {
	Write-Host "⚠️  La build no està completa. Revisa els errors i torna a compilar." -ForegroundColor Yellow
	exit 1
} else {
	Write-Host "ℹ️  Alguns warnings detectats, però els fitxers principals estan presents." -ForegroundColor Cyan
	exit 0
}
