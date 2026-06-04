# TEST IMMEDIAT - Execució cada 1 minut
# Aquest script configura el servei per executar cada minut i verifica que funciona

Write-Host "=== TEST RAPID QUARTZ FIX ===" -ForegroundColor Cyan
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

# 3. Iniciar servei
Write-Host "3. Iniciant servei amb nova versió..." -ForegroundColor Yellow
Start-Service MultirIntegraModulabService
Start-Sleep -Seconds 5

# 4. Veure logs inicials
Write-Host ""
Write-Host "4. LOGS D'INICI:" -ForegroundColor Green
Write-Host ""
$logsInici = Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 10
$logsInici | Format-Table TimeGenerated, EntryType, @{Label="Message";Expression={$_.Message}} -AutoSize -Wrap

# 5. Verificar propera execució
Write-Host ""
Write-Host "5. PROPERA EXECUCIÓ PROGRAMADA:" -ForegroundColor Cyan
$nextExec = $logsInici | Where-Object {$_.Message -like "*Propera execució*"} | Select-Object -First 1
if ($nextExec) {
	Write-Host $nextExec.Message -ForegroundColor White

	# Extreure hora
	if ($nextExec.Message -match "Propera execució: (.+)") {
		$horaText = $matches[1]
		Write-Host ""
		Write-Host "   Hauria d'executar-se a: $horaText" -ForegroundColor Yellow
	}
} else {
	Write-Host "   [ERROR] No s'ha trobat informació de propera execució!" -ForegroundColor Red
}

# 6. Verificar estat del scheduler
Write-Host ""
Write-Host "6. ESTAT DEL SCHEDULER:" -ForegroundColor Cyan
$estatScheduler = $logsInici | Where-Object {$_.Message -like "*Scheduler estat*"} | Select-Object -First 1
if ($estatScheduler) {
	Write-Host $estatScheduler.Message -ForegroundColor White
} else {
	Write-Host "   [WARNING] No s'ha trobat informació de l'estat del scheduler" -ForegroundColor Yellow
}

# 7. Esperar fins al proper minut
Write-Host ""
Write-Host "7. ESPERANT EXECUCIÓ AUTOMÀTICA..." -ForegroundColor Yellow
$ara = Get-Date
$segonsFinsPropMinut = 60 - $ara.Second + 5  # Esperar fins al proper minut + 5 segons marge

Write-Host "   Hora actual: $($ara.ToString('HH:mm:ss'))" -ForegroundColor Gray
Write-Host "   Esperant $segonsFinsPropMinut segons..." -ForegroundColor Gray

for ($i = $segonsFinsPropMinut; $i -gt 0; $i--) {
	if ($i % 10 -eq 0 -or $i -le 5) {
		Write-Host "   $i segons..." -ForegroundColor Gray
	}
	Start-Sleep -Seconds 1
}

# 8. Verificar si s'ha executat
Write-Host ""
Write-Host "8. VERIFICANT EXECUCIÓ:" -ForegroundColor Green
Write-Host ""

$logsPost = Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 15
$execucions = $logsPost | Where-Object {$_.Message -like "*Iniciant processament*"}

if ($execucions -and $execucions[0].TimeGenerated -gt $ara) {
	Write-Host "   ✅ [ÈXIT] S'HA EXECUTAT AUTOMÀTICAMENT!" -ForegroundColor Green
	Write-Host "   Hora execució: $($execucions[0].TimeGenerated.ToString('HH:mm:ss'))" -ForegroundColor Green

	# Buscar resultat
	Start-Sleep -Seconds 2
	$resultats = Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 5 | 
				  Where-Object {$_.Message -like "*finalitzat*"}

	if ($resultats) {
		Write-Host "   ✅ Processament finalitzat correctament" -ForegroundColor Green
	}

	Write-Host ""
	Write-Host "   🎉 QUARTZ FUNCIONA CORRECTAMENT! 🎉" -ForegroundColor Green

} else {
	Write-Host "   ❌ [ERROR] NO S'HA EXECUTAT!" -ForegroundColor Red
	Write-Host ""
	Write-Host "   Últims logs:" -ForegroundColor Yellow
	$logsPost | Select-Object -First 8 | Format-Table TimeGenerated, EntryType, Message -AutoSize -Wrap
}

Write-Host ""
Write-Host "=== FI DEL TEST ===" -ForegroundColor Cyan
Write-Host ""

# 9. Preguntar si restaurar configuració original
$restaurar = Read-Host "Vols restaurar la configuració original (cada 15 minuts)? (S/N)"
if ($restaurar -eq 'S' -or $restaurar -eq 's') {
	Write-Host "Restaurant configuració original..." -ForegroundColor Yellow

	@'
[
  {
	"workflowFile": "workflow-processar-mostres-modulab",
	"cron": "0 0/15 * * * ?",
	"description": "Processar mostres de Modulab cada 15 minuts",
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

	Restart-Service MultirIntegraModulabService
	Write-Host "✅ Configuració restaurada i servei reiniciat" -ForegroundColor Green
}
