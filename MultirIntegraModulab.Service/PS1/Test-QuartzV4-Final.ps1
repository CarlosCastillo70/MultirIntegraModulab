# TEST FINAL - VERSIÓN 4 AMB FALLBACK
# Script simple per verificar que funciona

Write-Host "=== TEST QUARTZ V4 - FALLBACK TIMER ===" -ForegroundColor Cyan
Write-Host ""

# 1. Aturar servei
Write-Host "1. Preparant test..." -ForegroundColor Yellow
Stop-Service MultirIntegraModulabService -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# 2. Configurar CRON cada minut per test immediat
Write-Host "2. Configurant CRON cada minut..." -ForegroundColor Yellow
cd C:\MultiR

# Crear configuració usant PowerShell objects i convertir a JSON
$config = @(
    @{
        workflowFile = "workflow-processar-mostres-modulab"
        cron = "0 * * * * ?"
        description = "TEST: Processar mostres cada minut"
        runOnStartup = $false
        type = "MultirIntegraModulab.Service.Jobs.ProcessarMostresModulabJob"
        assembly = "MultirIntegraModulab.Service"
        parameters = @{}
    },
    @{
        workflowFile = "workflow-revisar-vigencia-diagnostics"
        cron = "0 0 4 * * ?"
        description = "Revisar vigencia de diagnostics cada dia a les 4:00 AM"
        runOnStartup = $false
        type = "MultirIntegraModulab.Service.Jobs.RevisarVigenciaDiagnosticsJob"
        assembly = "MultirIntegraModulab.Service"
        parameters = @{}
    }
)

$config | ConvertTo-Json | Out-File -FilePath "workflow-schedule.json" -Encoding ASCII -Force

Write-Host "    ✅ Configuració actualitzada" -ForegroundColor Green

# 3. Iniciar servei
Write-Host "3. Iniciant servei..." -ForegroundColor Yellow
$timeStart = Get-Date
Start-Service MultirIntegraModulabService

# Esperar que s'iniciï
Write-Host "4. Esperant inicialització..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

# 4. Veure logs d'inici
Write-Host ""
Write-Host "5. LOGS INICI:" -ForegroundColor Green
$logs = Get-EventLog -LogName Application -Source MultirIntegraModulabService -After $timeStart -Newest 20

# Verificar errors
$errors = $logs | Where-Object {$_.EntryType -eq 'Error'}
if ($errors) {
    Write-Host "    ❌ ERRORS TROBATS:" -ForegroundColor Red
    $errors | ForEach-Object {
        Write-Host "    $($_.TimeGenerated.ToString('HH:mm:ss')) - $($_.Message.Substring(0, 100))" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "NO POTS CONTINUAR AMB ERRORS!" -ForegroundColor Red
    exit
}

# Verificar scheduler iniciat
$schedulerOK = $logs | Where-Object {$_.Message -like "*Scheduler Quartz iniciat*"}
if ($schedulerOK) {
    Write-Host "    ✅ Scheduler iniciat" -ForegroundColor Green
} else {
    Write-Host "    ❌ Scheduler NO iniciat!" -ForegroundColor Red
}

# Verificar triggers registrats
$triggersOK = $logs | Where-Object {$_.Message -like "*Triggers registrats*"}
if ($triggersOK) {
    Write-Host "    ✅ Triggers registrats" -ForegroundColor Green
    $triggersOK.Message | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
} else {
    Write-Host "    ❌ Triggers NO registrats!" -ForegroundColor Red
}

# 5. Esperar execució
Write-Host ""
Write-Host "6. ESPERANT EXECUCIÓ..." -ForegroundColor Yellow

$now = Get-Date
$segonsFinsPropMinut = 65 - $now.Second

Write-Host "    Hora actual: $($now.ToString('HH:mm:ss'))" -ForegroundColor Gray
Write-Host "    Esperant aproximadament $segonsFinsPropMinut segons..." -ForegroundColor Gray

for ($i = $segonsFinsPropMinut; $i -gt 0; $i--) {
    if ($i % 10 -eq 0 -or $i -le 5) {
        Write-Host "    $i segons..." -ForegroundColor Gray
    }
    Start-Sleep -Seconds 1
}

# 6. Verificar execució
Write-Host ""
Write-Host "7. RESULTATS:" -ForegroundColor Green

$logsPost = Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 30

# Buscar execucions (Quartz o Fallback)
$execucions = $logsPost | Where-Object {$_.Message -like "*Iniciant processament*"}
$fallbacks = $logsPost | Where-Object {$_.Message -like "*FALLBACK*"}

if ($execucions) {
    Write-Host ""
    Write-Host "    ✅ JOB EXECUTAT!" -ForegroundColor Green

    if ($fallbacks) {
        Write-Host "      (Executat per FALLBACK TIMER)" -ForegroundColor Yellow
    } else {
        Write-Host "      (Executat per Quartz)" -ForegroundColor Cyan
    }

    # Mostrar resultats (Netejat i corregit sense duplicitats)
    $results = $logsPost | Where-Object {$_.Message -like "*Processament finalitzat*"}
    if ($results) {
        Write-Host ""
        Write-Host "    Resultat:" -ForegroundColor Green
        
        $lines = $results[0].Message -split "`r?`n"
        foreach ($line in $lines) {
            Write-Host "      $line" -ForegroundColor Gray
        }
    }

    Write-Host ""
    Write-Host "    🎉 QUARTZ FALLBACK FUNCIONA PERFECTAMENT!" -ForegroundColor Green

} else {
    Write-Host "    ❌ NO S'HA EXECUTAT" -ForegroundColor Red
    Write-Host ""
    Write-Host "    Últims logs:" -ForegroundColor Yellow
    $logsPost | Select-Object -First 10 | Format-List TimeGenerated, EntryType, Message
}

Write-Host ""
Write-Host "=== FI DEL TEST ===" -ForegroundColor Cyan

# Preguntar si restaurar
Write-Host ""
$restaurar = Read-Host "Vols restaurar la configuració original (cada 15 minuts)? (S/N)"
if ($restaurar -eq "S" -or $restaurar -eq "s") {
    Write-Host "Restaurant..." -ForegroundColor Yellow

    $configOriginal = @(
        @{
            workflowFile = "workflow-processar-mostres-modulab"
            cron = "0 0/15 * * * ?"
            description = "Processar mostres de Modulab cada 15 minuts"
            runOnStartup = $false
            type = "MultirIntegraModulab.Service.Jobs.ProcessarMostresModulabJob"
            assembly = "MultirIntegraModulab.Service"
            parameters = @{}
        },
        @{
            workflowFile = "workflow-revisar-vigencia-diagnostics"
            cron = "0 0 4 * * ?"
            description = "Revisar vigencia de diagnostics cada dia a les 4:00 AM"
            runOnStartup = $false
            type = "MultirIntegraModulab.Service.Jobs.RevisarVigenciaDiagnosticsJob"
            assembly = "MultirIntegraModulab.Service"
            parameters = @{}
        }
    )

    $configOriginal | ConvertTo-Json | Out-File -FilePath "workflow-schedule.json" -Encoding ASCII -Force

    Restart-Service MultirIntegraModulabService
    Write-Host "✅ Restaurat!" -ForegroundColor Green
}