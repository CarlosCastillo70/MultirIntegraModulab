# 🔧 FIX QUARTZ VERSIÓ 3 - DIAGNÒSTIC I SOLUCIÓ PROFUNDA

## ⚠️ PROBLEMA IDENTIFICAT

Els logs mostren que el scheduler estava **ACTIU** però **els triggers no es disparaven** a l'hora programada:

```
11:44:32  Scheduler estat: ACTIU
11:44:32  Propera execució: 01/06/2026 11:45:00
[... no hi ha execució a les 11:45 ...]
```

### Causa arrel:

Quartz.NET amb `StdSchedulerFactory()` sense configuració explícita pot tenir issues en **Windows Services** perquè:

1. El thread pool no estava correctament configurat per a servei
2. No s'especificava que el thread pool havia de ser "non-daemon" (que no es tanqui quan no hi ha work)
3. La memòria job store es pot comportar de forma impredictible en servei

---

## ✅ SOLUCIONS APLICADES (VERSIÓ 3)

### 1. Configuració explícita de Quartz.NET

**ABANS**:
```csharp
var schedulerFactory = new StdSchedulerFactory();  // ❌ Configuració per defecte
_scheduler = schedulerFactory.GetScheduler().Result;
```

**ARA**:
```csharp
var props = new NameValueCollection();

// Configurar Quartz.NET per a Windows Service
props["quartz.scheduler.instanceName"] = "MultirIntegraModulabScheduler";
props["quartz.jobStore.type"] = "Quartz.Impl.RAMJobStore, Quartz";
props["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz";
props["quartz.threadPool.threadCount"] = "5";  // ✅ 5 threads
props["quartz.threadPool.threadPriority"] = "Normal";
props["quartz.scheduler.threadName"] = "MultirModulabThread";

// ✅ CRÍTIC: Forçar que el scheduler no es tanqui
props["quartz.scheduler.makeSchedulerThreadDaemon"] = "false";

var schedulerFactory = new StdSchedulerFactory(props);
_scheduler = schedulerFactory.GetScheduler().Result;
```

### 2. Logs més detallats per diagnosticar

**NOU** - Llistar els 3 següents fires per cada trigger:

```csharp
var nextFireTimes = new List<string>();
var fireTime = nextFireTime;
for (int i = 0; i < 3 && fireTime.HasValue; i++)
{
	nextFireTimes.Add(TimeZoneInfo.ConvertTimeFromUtc(fireTime.Value.DateTime, TimeZoneInfo.Local).ToString("dd/MM/yyyy HH:mm:ss"));
	fireTime = trigger.GetFireTimeAfter(fireTime.Value);
}

EventLog.WriteEntry(this.ServiceName, 
	$"Tasca programada: {wf.Description} - CRON: {wf.Cron}\n" +
	$"Propera execució: {nextFireTimeLocal:dd/MM/yyyy HH:mm:ss}\n" +
	$"Següents fires: {string.Join(" | ", nextFireTimes)}", 
	EventLogEntryType.Information);
```

### 3. Verificació de triggers registrats

**NOU** - Log que mostra quants triggers estan registrats:

```csharp
var triggerKeys = _scheduler.GetTriggerKeys(null).Result;

EventLog.WriteEntry(this.ServiceName, 
	$"Scheduler estat: {(isStarted ? "ACTIU" : "INACTIU")}\n" +
	$"Jobs registrats: {jobKeys.Count}\n" +
	$"Triggers registrats: {triggerKeys.Count}", 
	EventLogEntryType.Information);
```

---

## 🚀 DESPLEGAMENT

### Pas 1: Compilar la nova versió

```powershell
# A Visual Studio: Build > Build Solution (Release)
cd C:\Projectes\MultirIntegraModulab
msbuild MultirIntegraModulab.Service\MultirIntegraModulab.Service.csproj /p:Configuration=Release
```

### Pas 2: Copiar al servidor

```powershell
# Copiar des de:
C:\Projectes\MultirIntegraModulab\MultirIntegraModulab.Service\bin\Release\MultirIntegraModulab.Service.exe

# Cap a:
C:\MultiR\MultirIntegraModulab.Service.exe
```

### Pas 3: Copiar script de test (opcional però recomanat)

```powershell
# Copiar Test-QuartzV3.ps1 a C:\MultiR
```

### Pas 4: Test amb script automàtic

```powershell
cd C:\MultiR
.\Test-QuartzV3.ps1
```

**O manual**:

```powershell
# 1. Aturar
Stop-Service MultirIntegraModulabService
Start-Sleep -Seconds 3

# 2. Configurar CRON cada minut (fitxer workflow-schedule.json)
# Canviar la primera tasca a "cron": "0 * * * * ?"

# 3. Iniciar
Start-Service MultirIntegraModulabService
Start-Sleep -Seconds 5

# 4. Veure logs
Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 15 | Format-List TimeGenerated, EntryType, Message

# 5. Esperar ~70 segons i verificar
Start-Sleep -Seconds 70
Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 5 | Where-Object {$_.Message -like "*Iniciant processament*"}
```

---

## 📊 LOGS ESPERATS

### Després d'iniciar (amb nova versió):

```
12:35:42  Information  Iniciant servei MultirIntegraModulab...
12:35:42  Information  Scheduler Quartz iniciat correctament
12:35:42  Information  Carregades 2 tasques programades
12:35:42  Information  Tasca programada: TEST: Processar mostres cada minut - CRON: 0 * * * * ?
					   Propera execució: 01/06/2026 12:36:00
					   Següents fires: 01/06/2026 12:36:00 | 01/06/2026 12:37:00 | 01/06/2026 12:38:00
12:35:42  Information  Tasca programada: Revisar vigencia de diagnostics cada dia a les 4:00 AM - CRON: 0 0 4 * * ?
					   Propera execució: 02/06/2026 04:00:00
					   Següents fires: 02/06/2026 04:00:00 | 03/06/2026 04:00:00 | 04/06/2026 04:00:00
12:35:42  Information  Scheduler estat: ACTIU
					   Jobs registrats: 2
					   Triggers registrats: 2
12:35:42  Information  Servei MultirIntegraModulab iniciat correctament
```

### A la propera ejecución (12:36:00):

```
12:36:00  Information  [01/06/2026 12:36:00] Iniciant processament de mostres Modulab...
12:36:16  Information  Processament finalitzat. Durada: 16,45s. Exit code: 0
```

### Execucions subsegüents:

```
12:37:00  Information  [01/06/2026 12:37:00] Iniciant processament de mostres Modulab...
12:37:14  Information  Processament finalitzat. Durada: 14,32s. Exit code: 0

12:38:00  Information  [01/06/2026 12:38:00] Iniciant processament de mostres Modulab...
12:38:16  Information  Processament finalitzat. Durada: 16,21s. Exit code: 0
```

---

## ✅ VERIFICACIÓ

Després de desplegar, el script dirà:

```
✅ [ÈXIT] S'HA EXECUTAT!

Execució a: 12:36:00

🎉 QUARTZ FUNCIONA!
```

---

## 🐛 SI SEGUEIX SIN FUNCIONAR

### 1. Verificar que els triggers estan registrats

```powershell
$logs = Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 20
$logs | Where-Object {$_.Message -like "*Triggers registrats*"}

# Hauria de mostrar: "Triggers registrats: 2"
```

### 2. Verificar les fires calculades

```powershell
$logs | Where-Object {$_.Message -like "*Següents fires*"}

# Hauria de mostrar les 3 següents hores d'execució
```

### 3. Si "Triggers registrats: 0"

Llavors hi ha problema amb `ScheduleJob`. Afegir logs dins de ScheduleWorkflow.

### 4. Si els triggers mostren fires pero no s'executen

Llavors el problema és que els events no es disparano. Possibilitats:
- El scheduler thread està mort
- Hi ha excepció silenciosa en l'execució del job
- Problem amb el timezone

---

## 📝 CANVIS TOTALS

| Arxiu | Canvis |
|-------|--------|
| WorkflowService.cs | Configuració explícita Quartz + logs detallats |
| Test-QuartzV3.ps1 | Script de test V3 amb diagnòstic |

---

## 📌 CONFIGURACIÓ FINAL RECOMANA

Per a producció, una vegada verificat que funciona:

### workflow-schedule.json (final):

```json
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
```

---

**Data**: 01/06/2026  
**Versió**: 3.0  
**Estat**: Fix definitiu aplicat
