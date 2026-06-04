# TEST IMMEDIAT VERSIÓ 3 - Diagnòstic complet
# Este script prova Quartz amb logs detallats

Write-Host "=== TEST QUARTZ V3 - DIAGNÒSTIC COMPLET ===" -ForegroundColor Cyan
Write-Host ""

# 1. Aturar servei
Write-Host "1. Aturant servei..." -ForegroundColor Yellow
Stop-Service MultirIntegraModulabService -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# 2. Configurar CRON cada 1 minut
Write-Host "2. Configurant execució cada 1 minut per test..." -ForegroundColor Yellow
cd C:\MultiR

@'
[
  {
	"workflowFile": "workflow-processar-mostres-modulab",
	"cron": "0 * * * * ?",
	"description": "TEST: Processar mostres cada minut",
	"runOnStartup": false,
	"type": "MultirIntegraModulab.Service.Jobs.ProcessarMostresModulabJob",
	"assembly": "MultirIntegraModulab.Service",
	"parameters": {}
  },
  {
	"workflowFile": "workflow-revisar-vigencia-diagnostics",
	"cron": "0 0 4 * * ?",
	"description": "Revisar vigencia de diagnostics cada dia a les 4:00 AM",
	"runOnStartup": false,
	"type": "MultirIntegraModulab.Service.Jobs.RevisarVigenciaDiagnosticsJob",
	"assembly": "MultirIntegraModulab.Service",
	"parameters": {}
  }
]
'@ | Out-File -FilePath "workflow-schedule.json" -Encoding ASCII -Force

Write-Host "   ✅ Configuració actualitzada"

# 3. Iniciar servei
Write-Host "3. Iniciant servei..." -ForegroundColor Yellow
$timeStart = Get-Date
Start-Service MultirIntegraModulabService
Start-Sleep -Seconds 5

# 4. Veure logs d'inici complets
Write-Host ""
Write-Host "4. LOGS D'INICI DETALLATS:" -ForegroundColor Green
Write-Host ""

$logsInici = Get-EventLog -LogName Application -Source MultirIntegraModulabService -After $timeStart -Newest 20

# Mostrar tots els logs
Write-Host "Tots els logs d'inici:" -ForegroundColor Cyan
$logsInici | Format-List TimeGenerated, EntryType, Message

# 5. Analitzar logs
Write-Host ""
Write-Host "5. ANÁLISI DELS LOGS:" -ForegroundColor Cyan
Write-Host ""

# Verificar scheduler iniciat
$schedulerInicio = $logsInici | Where-Object {$_.Message -like "*Scheduler Quartz iniciat*"} | Select-Object -First 1
if ($schedulerInicio) {
	Write-Host "   ✅ Scheduler iniciat: $($schedulerInicio.TimeGenerated.ToString('HH:mm:ss'))" -ForegroundColor Green
} else {
	Write-Host "   ❌ Scheduler NO iniciat!" -ForegroundColor Red
}

# Verificar estat scheduler
$schedulerEstat = $logsInici | Where-Object {$_.Message -like "*Scheduler estat*"} | Select-Object -First 1
if ($schedulerEstat) {
	Write-Host "   📊 Estat scheduler:" -ForegroundColor Yellow
	$schedulerEstat.Message | ForEach-Object {
		$lines = $_.Split("`n")
		foreach ($line in $lines) {
			Write-Host "      $line" -ForegroundColor Gray
		}
	}
} else {
	Write-Host "   ❌ No s'ha trobat informació d'estat!" -ForegroundColor Red
}

# Verificar triggers
$triggerLogs = $logsInici | Where-Object {$_.Message -like "*Tasca programada*"} | Select-Object -First 2
if ($triggerLogs) {
	Write-Host "   📅 Triggers programats:" -ForegroundColor Yellow
	$triggerLogs | ForEach-Object {
		Write-Host ""
		$_.Message | ForEach-Object {
			$lines = $_.Split("`n")
			foreach ($line in $lines) {
				Write-Host "      $line" -ForegroundColor Gray
			}
		}
	}
} else {
	Write-Host "   ❌ No s'han trobat triggers!" -ForegroundColor Red
}

# 6. Esperar fins al proper minut
Write-Host ""
Write-Host "6. ESPERANT EXECUCIÓ AUTOMÀTICA..." -ForegroundColor Yellow

$ara = Get-Date
$segonsFinsPropMinut = 65 - $ara.Second  # Esperar fins al proper minut + 5 segons marge

Write-Host "   Hora actual: $($ara.ToString('HH:mm:ss'))" -ForegroundColor Gray
Write-Host "   Esperant aproximadament $segonsFinsPropMinut segons per veure l'execució automàtica..." -ForegroundColor Gray
Write-Host ""

$timeExecEsperada = $ara.AddSeconds($segonsFinsPropMinut)
Write-Host "   Execució esperada aproximadament a: $($timeExecEsperada.ToString('HH:mm:ss'))" -ForegroundColor Yellow

for ($i = $segonsFinsPropMinut; $i -gt 0; $i--) {
	if ($i % 10 -eq 0 -or $i -le 5) {
		Write-Host "   $i segons..." -ForegroundColor Gray
	}
	Start-Sleep -Seconds 1
}

# 7. Verificar si s'ha executat
Write-Host ""
Write-Host "7. RESULTATS DESPRÉS DE L'ESPERA:" -ForegroundColor Green
Write-Host ""

$logsPost = Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 30

# Buscar execucions
$execucions = $logsPost | Where-Object {$_.Message -like "*Iniciant processament*"}

if ($execucions) {
	Write-Host "   ✅ [ÈXIT] S'HA EXECUTAT!" -ForegroundColor Green
	Write-Host ""

	$execucions | ForEach-Object {
		Write-Host "   Execució a: $($_.TimeGenerated.ToString('HH:mm:ss'))" -ForegroundColor Green
	}

	Write-Host ""
	Write-Host "   Detalls de l'execució:" -ForegroundColor Cyan
	$logsPost | Where-Object {$_.Message -like "*Processament finalitzat*"} | Select-Object -First 1 | ForEach-Object {
		$_.Message | ForEach-Object {
			$lines = $_.Split("`n")
			foreach ($line in $lines) {
				Write-Host "      $line" -ForegroundColor Gray
			}
		}
	}

	Write-Host ""
	Write-Host "   🎉 QUARTZ FUNCIONA!" -ForegroundColor Green

} else {
	Write-Host "   ❌ [ERROR] NO S'HA EXECUTAT!" -ForegroundColor Red
	Write-Host ""
	Write-Host "   Tots els logs posteriors a l'inici:" -ForegroundColor Yellow
	Write-Host ""

	$logsPost | Format-List TimeGenerated, EntryType, Message
}

Write-Host ""
Write-Host "=== FI DEL TEST ===" -ForegroundColor Cyan
