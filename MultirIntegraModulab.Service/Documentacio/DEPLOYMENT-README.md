# MultirIntegraModulab Windows Service - Guia de Desplegament

## 📋 Descripció

Windows Service que executa automàticament:
- **ProcessarMostresModulabJob**: Cada 15 minuts
- **RevisarVigenciaDiagnosticsJob**: Cada dia a les 4:00 AM

## 🔧 Requisits

- Windows Server o Windows 10/11
- .NET Framework 4.8
- Permisos d'Administrador
- PowerShell 5.0 o superior

## 📦 Fitxers Necessaris

Assegureu-vos que aquests fitxers estiguin al mateix directori:

```
MultirIntegraModulab.Service/
├── MultirIntegraModulab.Service.exe         (Executable del servei)
├── MultirIntegraModulab.exe                 (Executable del processament de mostres)
├── MultirRevisioVigencia.exe                (Executable de revisió de vigència)
├── workflow-schedule.json                    (Configuració de tasques)
├── Quartz.dll                                (Dependència)
├── Newtonsoft.Json.dll                       (Dependència)
├── Microsoft.Extensions.Logging.Abstractions.dll (Dependència)
└── *.config i altres DLLs necessàries
```

## 🚀 Instal·lació

### Opció 1: Amb Script PowerShell (Recomanat)

1. Obrir PowerShell com a **Administrador**
2. Navegar al directori del servei
3. Executar el script d'instal·lació:

```powershell
cd C:\Path\To\MultirIntegraModulab.Service
.\Install-Service.ps1
```

### Opció 2: Manual amb sc.exe

```cmd
sc create MultirIntegraModulabService binPath= "C:\Path\To\MultirIntegraModulab.Service.exe" start= auto DisplayName= "MultiR Integra Modulab Service"
sc description MultirIntegraModulabService "Servei per executar periòdicament la integració de mostres Modulab i la revisió de vigència de diagnòstics"
sc failure MultirIntegraModulabService reset= 86400 actions= restart/60000/restart/60000/restart/60000
sc start MultirIntegraModulabService
```

## ⚙️ Configuració

### Modificar Freqüència d'Execució

Editar `workflow-schedule.json`:

```json
[
  {
	"workflowFile": "workflow-processar-mostres-modulab",
	"cron": "0 0/15 * * * ?",    // Cada 15 minuts (canviar aquí)
	"description": "Processar mostres de Modulab cada 15 minuts",
	"runOnStartup": false,
	"type": "MultirIntegraModulab.Service.Jobs.ProcessarMostresModulabJob",
	"assembly": "MultirIntegraModulab.Service",
	"parameters": {}
  },
  {
	"workflowFile": "workflow-revisar-vigencia-diagnostics",
	"cron": "0 0 4 * * ?",        // Cada dia a les 4:00 AM (canviar aquí)
	"description": "Revisar vigència de diagnòstics cada dia a les 4:00 AM",
	"runOnStartup": false,
	"type": "MultirIntegraModulab.Service.Jobs.RevisarVigenciaDiagnosticsJob",
	"assembly": "MultirIntegraModulab.Service",
	"parameters": {}
  }
]
```

### Expressions CRON de Quartz

Format: `segons minuts hores dia mes dia_setmana [any]`

Exemples:
- `0 0/15 * * * ?` - Cada 15 minuts
- `0 0 4 * * ?` - Cada dia a les 4:00 AM
- `0 0 */2 * * ?` - Cada 2 hores
- `0 30 8 * * ?` - Cada dia a les 8:30 AM
- `0 0 12 ? * MON-FRI` - De dilluns a divendres a les 12:00 PM

## 🔄 Gestió del Servei

### Iniciar el servei
```powershell
Start-Service MultirIntegraModulabService
```

### Aturar el servei
```powershell
Stop-Service MultirIntegraModulabService
```

### Reiniciar el servei
```powershell
Restart-Service MultirIntegraModulabService
```

### Verificar estat
```powershell
Get-Service MultirIntegraModulabService
```

### Des de Services.msc
1. Windows + R → `services.msc`
2. Buscar "MultiR Integra Modulab Service"
3. Clic dret → Iniciar/Aturar/Reiniciar

## 🗑️ Desinstal·lació

### Amb Script PowerShell
```powershell
.\Uninstall-Service.ps1
```

### Manual
```cmd
sc stop MultirIntegraModulabService
sc delete MultirIntegraModulabService
```

## 📊 Monitorització

### Event Viewer (Visor d'Esdeveniments)

1. Windows + R → `eventvwr.msc`
2. Navegar a: **Registres de Windows > Application**
3. Filtrar per origen:
   - `MultirIntegraModulabService` - Logs del servei i processament Modulab
   - `MultirRevisioVigenciaService` - Logs de revisió de vigència

### Tipus de logs generats:

- **Information**: Inici/aturada del servei, execucions correctes
- **Warning**: Warnings durant l'execució
- **Error**: Errors en el servei o en les tasques

### Exemple de logs:
```
[25/01/2026 10:15:00] Iniciant processament de mostres Modulab...
Processament finalitzat. Durada: 12.34s. Exit code: 0

[25/01/2026 04:00:00] Iniciant revisió de vigència de diagnòstics...
Revisió finalitzada. Durada: 5.67s. Exit code: 0
```

## 🐛 Resolució de Problemes

### El servei no inicia

1. Verificar que tots els fitxers necessaris estan presents
2. Revisar els logs d'esdeveniments
3. Verificar permisos del directori
4. Comprovar que .NET Framework 4.8 està instal·lat

### Els executables no es troben

Assegureu-vos que `MultirIntegraModulab.exe` i `MultirRevisioVigencia.exe` estan al mateix directori que el servei.

### Les tasques no s'executen

1. Verificar que el servei està en execució
2. Revisar `workflow-schedule.json` per errors de sintaxi
3. Comprovar expressions CRON
4. Revisar logs d'esdeveniments

### Canviar configuració no té efecte

Després de modificar `workflow-schedule.json`, cal **reiniciar el servei**:
```powershell
Restart-Service MultirIntegraModulabService
```

## ✅ Checklist Pre-Producció

- [ ] Tots els executables estan compilats en mode **Release**
- [ ] `MultirIntegraModulab.exe` i `MultirRevisioVigencia.exe` estan al directori del servei
- [ ] Fitxer `workflow-schedule.json` està present
- [ ] Totes les DLLs de Quartz i dependències estan presents
- [ ] S'han configurat les expressions CRON correctes
- [ ] S'ha provat la instal·lació en un entorn de test
- [ ] S'ha verificat que els logs es generen correctament
- [ ] S'ha configurat recuperació automàtica del servei
- [ ] S'ha documentat el path d'instal·lació per a operacions

## 📝 Notes de Producció

- **Inici automàtic**: El servei està configurat per iniciar automàticament amb Windows
- **Recuperació**: En cas de fallada, el servei es reiniciarà automàticament (3 intents amb 60s de delay)
- **Logs**: Es recomana revisar periòdicament els logs per detectar problemes
- **Backups**: Fer còpia de seguretat de `workflow-schedule.json` abans de fer canvis

## 📞 Suport

Per problemes o dubtes, revisar:
1. Logs del Event Viewer
2. Aquest document
3. Repositori: https://github.com/CarlosCastillo70/MultirIntegraModulab

---

**Versió**: 1.0.0  
**Data**: 2026  
**Autor**: ICS
