# 🔧 FIX APLICAT: Problema amb Quartz.NET (VERSIÓ 2)

## 📋 PROBLEMA

El servei executava correctament amb `runOnStartup: true` però **no s'executava automàticament** amb la programació CRON.

**Símptomes**:
- Logs mostren "Propera execució: [hora]" ✅
- Servei en estat "Running" ✅
- Però no s'executa res quan arriba l'hora ❌

**Causa identificada**: Tres problemes combinats:
1. El scheduler s'iniciava de forma asíncrona sense esperar completament (`_scheduler.Start()`)
2. El trigger **no estava vinculat explícitament** amb el job (`.ForJob()` absent)
3. No s'especificava el **timezone explícitament** (possibles problemes UTC vs Local)

---

## ✅ SOLUCIÓ APLICADA (VERSIÓ 2 - DEFINITIVA)

### Canvis al codi (`WorkflowService.cs`):

#### 1. Esperar iniciació completa del Scheduler

**ABANS**:
```csharp
_scheduler = schedulerFactory.GetScheduler().Result;
_scheduler.Start();  // No espera!
```

**ARA**:
```csharp
_scheduler = schedulerFactory.GetScheduler().Result;
_scheduler.Start().Wait();  // ✅ Espera que s'iniciï completament
EventLog.WriteEntry(this.ServiceName, 
	"Scheduler Quartz iniciat correctament", 
	EventLogEntryType.Information);
```

#### 2. Vincular Trigger amb Job + Timezone explícit

**ABANS**:
```csharp
var trigger = TriggerBuilder.Create()
	.WithIdentity($"{wf.WorkflowFile}-trigger")
	.WithCronSchedule(wf.Cron)  // ❌ Falta vinculació i timezone!
	.WithDescription(wf.Description)
	.Build();
```

**ARA**:
```csharp
var trigger = TriggerBuilder.Create()
	.WithIdentity($"{wf.WorkflowFile}-trigger")
	.ForJob(job)  // ✅ Vincular explícitament amb el job
	.WithCronSchedule(wf.Cron, x => x
		.InTimeZone(TimeZoneInfo.Local))  // ✅ Timezone explícit (Local)
	.WithDescription(wf.Description)
	.Build();
```

**Per què és important?**
- `.ForJob(job)`: Quartz necessita saber quin Job ha d'executar el trigger
- `.InTimeZone(TimeZoneInfo.Local)`: Assegura que "11:30" significa 11:30 hora local, no UTC

#### 3. Verificar estat del Scheduler

**NOU** - Log per confirmar que el scheduler està actiu:

```csharp
var isStarted = _scheduler.IsStarted;
var jobKeys = _scheduler.GetJobKeys(null).Result;

EventLog.WriteEntry(this.ServiceName, 
	$"Scheduler estat: {(isStarted ? "ACTIU" : "INACTIU")}\n" +
	$"Jobs programats: {jobKeys.Count}", 
	EventLogEntryType.Information);
```

#### 4. Logs de Propera Execució

**ARA** els logs mostren la **propera execució prevista**:

```csharp
var nextFireTime = trigger.GetNextFireTimeUtc();
var nextFireTimeLocal = nextFireTime.HasValue ? 
	TimeZoneInfo.ConvertTimeFromUtc(nextFireTime.Value.DateTime, TimeZoneInfo.Local) : 
	DateTime.MinValue;

EventLog.WriteEntry(this.ServiceName, 
	$"Tasca programada: {wf.Description} - CRON: {wf.Cron}\n" +
	$"Propera execució: {nextFireTimeLocal:dd/MM/yyyy HH:mm:ss}", 
	EventLogEntryType.Information);
```

---

## 🚀 DESPLEGAMENT RÀPID

### Opció A: Test immediat amb script automàtic

```powershell
# Al servidor C:\MultiR
# 1. Copiar la nova versió de MultirIntegraModulab.Service.exe
# 2. Executar:
.\Test-QuartzFix.ps1
```

Aquest script:
- Atura el servei
- Configura execució **cada 1 minut** (per test)
- Inicia el servei
- Espera i verifica que s'executi automàticament
- Et pregunta si vols restaurar la configuració original (cada 15 min)

### Opció B: Desplegament manual

#### Pas 1: Compilar la nova versió

```powershell
# A la màquina de desenvolupament
cd C:\Projectes\MultirIntegraModulab
# Build > Build Solution (Release)
```

#### Pas 2: Copiar al servidor

```powershell
# Copiar des de:
C:\Projectes\MultirIntegraModulab\MultirIntegraModulab.Service\bin\Release\MultirIntegraModulab.Service.exe

# Cap a:
C:\MultiR\MultirIntegraModulab.Service.exe
```

#### Pas 3: Reiniciar servei

```powershell
# Al servidor C:\MultiR
Restart-Service MultirIntegraModulabService
Start-Sleep -Seconds 5
```

#### Pas 4: Verificar logs

```powershell
Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 10 | 
	Format-Table TimeGenerated, EntryType, Message -AutoSize -Wrap
```

**Hauries de veure**:

```
11:35:42  Information  Iniciant servei MultirIntegraModulab...
11:35:42  Information  Scheduler Quartz iniciat correctament
11:35:42  Information  Carregades 2 tasques programades
11:35:42  Information  Tasca programada: Processar mostres de Modulab cada 15 minuts - CRON: 0 0/15 * * * ?
					   Propera execució: 01/06/2026 11:45:00
11:35:42  Information  Tasca programada: Revisar vigencia de diagnostics cada dia a les 4:00 AM - CRON: 0 0 4 * * ?
					   Propera execució: 02/06/2026 04:00:00
11:35:42  Information  Scheduler estat: ACTIU          ← ✅ IMPORTANT!
					   Jobs programats: 2
11:35:42  Information  Servei MultirIntegraModulab iniciat correctament
```

#### Pas 5: Esperar l'execució

Espera fins a l'hora indicada a "Propera execució" i verifica:

```powershell
# Després de l'hora programada
Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 5
```

**Hauries de veure**:

```
11:45:00  Information  [01/06/2026 11:45:00] Iniciant processament de mostres Modulab...
11:45:16  Information  Processament finalitzat. Durada: 16,32s. Exit code: 0
```

---

## 🧪 TEST RÀPID (Cada 1 minut)

Si vols verificar **immediatament** sense esperar 15 minuts:

### 1. Configurar CRON cada minut

```powershell
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
```

### 2. Reiniciar servei

```powershell
Restart-Service MultirIntegraModulabService
Start-Sleep -Seconds 5
```

### 3. Esperar 70 segons

```powershell
Start-Sleep -Seconds 70
```

### 4. Verificar execució

```powershell
Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 5 | 
	Where-Object {$_.Message -like "*Iniciant processament*"}
```

Si veus un log recent amb "Iniciant processament..." **✅ FUNCIONA!**

### 5. Restaurar configuració original

```powershell
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
```

---

## 📊 LOGS ESPERATS

### Després d'iniciar el servei:

```
11:35:42  Information  Iniciant servei MultirIntegraModulab...
11:35:42  Information  Scheduler Quartz iniciat correctament
11:35:42  Information  Carregades 2 tasques programades
11:35:42  Information  Tasca programada: Processar mostres de Modulab cada 15 minuts - CRON: 0 0/15 * * * ?
					   Propera execució: 01/06/2026 11:45:00
11:35:42  Information  Tasca programada: Revisar vigencia de diagnostics cada dia a les 4:00 AM - CRON: 0 0 4 * * ?
					   Propera execució: 02/06/2026 04:00:00
11:35:42  Information  Scheduler estat: ACTIU
					   Jobs programats: 2
11:35:42  Information  Servei MultirIntegraModulab iniciat correctament
```

### A l'hora programada (11:45:00):

```
11:45:00  Information  [01/06/2026 11:45:00] Iniciant processament de mostres Modulab...
11:45:16  Information  Processament finalitzat. Durada: 16,32s. Exit code: 0
```

### Següent execució (12:00:00):

```
12:00:00  Information  [01/06/2026 12:00:00] Iniciant processament de mostres Modulab...
12:00:14  Information  Processament finalitzat. Durada: 14,58s. Exit code: 0
```

---

## ✅ CHECKLIST DE VERIFICACIÓ

Després de desplegar, comprova:

- [ ] El servei inicia correctament
- [ ] Apareix "Scheduler Quartz iniciat correctament"
- [ ] Apareix "Scheduler estat: ACTIU"
- [ ] Apareix "Jobs programats: 2"
- [ ] Apareix "Propera execució: [data/hora]"
- [ ] A l'hora indicada, s'executa automàticament
- [ ] Els logs mostren "Iniciant processament..." i "Processament finalitzat"
- [ ] Exit code: 0

---

## 🐛 SI ENCARA NO FUNCIONA

Si després d'aplicar aquest fix encara no s'executa:

### 1. Verificar que s'ha desplegat la nova versió

```powershell
# Comprovar data de modificació de l'executable
Get-Item C:\MultiR\MultirIntegraModulab.Service.exe | Select-Object FullName, LastWriteTime
```

### 2. Verificar logs crítics

```powershell
Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 20 | 
	Where-Object {$_.Message -like "*Scheduler estat*" -or $_.Message -like "*ACTIU*"}
```

**Hauria de mostrar**: "Scheduler estat: ACTIU"  
**Si mostra**: "Scheduler estat: INACTIU" → Hi ha un problema amb la inicialització

### 3. Verificar que no hi ha errors

```powershell
Get-EventLog -LogName Application -Source MultirIntegraModulabService -Newest 20 | 
	Where-Object {$_.EntryType -eq 'Error'}
```

### 4. Provar amb CRON cada minut

Seguir els passos del "TEST RÀPID" per veure si funciona amb execucions més freqüents.

### 5. Verificar hora del sistema

```powershell
Get-Date
```

Assegurar que l'hora del sistema és correcta i coincideix amb la "Propera execució".

---

## 📝 RESUM DELS CANVIS

| Component | Abans | Ara |
|-----------|-------|-----|
| Inicialització Scheduler | `_scheduler.Start()` | `_scheduler.Start().Wait()` |
| Vinculació Trigger-Job | Absent | `.ForJob(job)` |
| Timezone | Implícit (UTC?) | `.InTimeZone(TimeZoneInfo.Local)` |
| Log estat scheduler | Absent | `Scheduler estat: ACTIU` |
| Log propera execució | Format simple | Data/hora completa |

---

## 📞 SUPORT

Si després de seguir tots aquests passos encara no funciona, proporciona els següents logs:

```powershell
# Logs complets des de l'inici del servei
Get-EventLog -LogName Application -Source MultirIntegraModulabService -After (Get-Date).AddHours(-1) | 
	Format-List TimeGenerated, EntryType, Message
```

---

**Data del fix**: 01/06/2026  
**Versió**: 2.0 (FIX DEFINITIU)  
**Build requerit**: Sí, recompilar en Release  
**Fitxers modificats**: `WorkflowService.cs`  
**Fitxers nous**: `Test-QuartzFix.ps1` (script de test automàtic)
