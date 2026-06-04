# MultirIntegraModulab.Service

Windows Service per executar periòdicament:
- **MultirIntegraModulab**: Integració de mostres de Modulab (cada 15 minuts)
- **MultirRevisioVigencia**: Revisió de vigència de diagnòstics (1 cop al dia a les 4:00 AM)

## Tecnologies

- **.NET Framework 4.8**
- **Quartz.NET 3.6.2** - Programador de tasques amb expressions CRON
- **Windows Service** - Servei natiu de Windows

## Estructura del Projecte

```
MultirIntegraModulab.Service/
├── Jobs/                                    # Classes de tasques programades
│   ├── ProcessarMostresModulabJob.cs       # Executa cada 15 minuts
│   └── RevisarVigenciaDiagnosticsJob.cs    # Executa 1 cop al dia
├── Models/                                  # Models de dades
│   ├── WorkflowScheduleItem.cs             # Configuració d'una tasca
│   ├── WorkflowParameter.cs                # Paràmetres de tasques
│   └── JobExecutionContextMock.cs          # Context mock per execucions manuals
├── Services/                                # Serveis principals
│   ├── WorkflowService.cs                  # Windows Service principal
│   └── WorkflowService.Designer.cs         # Dissenyador del servei
├── Properties/
│   └── AssemblyInfo.cs
├── workflow-schedule.json                   # Configuració de programació (CRON)
├── App.config
├── packages.config
└── Program.cs                               # Punt d'entrada

```

## Configuració de Programació (CRON)

El fitxer `workflow-schedule.json` defineix quan s'executen les tasques:

### Format d'Expressió CRON

```
┌───────────── segons (0-59)
│ ┌───────────── minuts (0-59)
│ │ ┌───────────── hores (0-23)
│ │ │ ┌───────────── dia del mes (1-31)
│ │ │ │ ┌───────────── mes (1-12)
│ │ │ │ │ ┌───────────── dia de la setmana (0-7, 0=diumenge)
│ │ │ │ │ │
│ │ │ │ │ │
* * * * * ?
```

### Exemples d'Expressions CRON

| Expressió | Descripció |
|-----------|------------|
| `0 0/15 * * * ?` | Cada 15 minuts |
| `0 0 4 * * ?` | Cada dia a les 4:00 AM |
| `0 0 */2 * * ?` | Cada 2 hores |
| `0 30 9 * * MON-FRI` | De dilluns a divendres a les 9:30 AM |
| `0 0 0 1 * ?` | El primer dia de cada mes a mitjanit |

### Configuració Actual

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
	"description": "Revisar vigència de diagnòstics cada dia a les 4:00 AM",
	"runOnStartup": false,
	"type": "MultirIntegraModulab.Service.Jobs.RevisarVigenciaDiagnosticsJob",
	"assembly": "MultirIntegraModulab.Service",
	"parameters": {}
  }
]
```

## Instal·lació del Servei

### Pas 1: Compilar el projecte

```bash
# En mode Release
dotnet build --configuration Release
# o des de Visual Studio: Build > Build Solution (Release)
```

### Pas 2: Instal·lar el servei amb InstallUtil

```powershell
# Executar PowerShell com a Administrador
cd "C:\Projectes\MultirIntegraModulab\MultirIntegraModulab.Service\bin\Release"

# Instal·lar el servei
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe MultirIntegraModulab.Service.exe
```

### Pas 3: Iniciar el servei

```powershell
# Opció 1: Des de PowerShell
Start-Service -Name "MultirIntegraModulabService"

# Opció 2: Des de Services.msc
# 1. Obre services.msc
# 2. Cerca "MultiR Integra Modulab Service"
# 3. Fes clic dret > Start
```

## Desinstal·lació del Servei

```powershell
# Aturar el servei
Stop-Service -Name "MultirIntegraModulabService"

# Desinstal·lar el servei
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u MultirIntegraModulab.Service.exe
```

## Logs i Monitoratge

El servei escriu logs al **Event Viewer** de Windows:

1. Obre **Event Viewer** (eventvwr.msc)
2. Navega a: **Windows Logs > Application**
3. Filtra per fonts:
   - `MultirIntegraModulabService` - Gestió del servei i execució de Modulab
   - `MultirRevisioVigenciaService` - Execució de revisió de vigència

### Tipus de logs

- **Information** ✅ - Execucions correctes, inicis/aturades del servei
- **Warning** ⚠️ - Execucions amb errors no crítics
- **Error** ❌ - Errors crítics que impedeixen l'execució

## Modificar la Programació

Per canviar quan s'executen les tasques:

1. Edita `workflow-schedule.json` al directori del servei
2. Modifica les expressions CRON segons necessitat
3. Reinicia el servei:
   ```powershell
   Restart-Service -Name "MultirIntegraModulabService"
   ```

## Troubleshooting

### El servei no s'inicia

1. Comprova els logs al Event Viewer
2. Verifica que el fitxer `workflow-schedule.json` existeix i és vàlid
3. Assegura't que els executables `MultirIntegraModulab.exe` i `MultirRevisioVigencia.exe` existeixen

### Les tasques no s'executen

1. Comprova que l'expressió CRON és correcta
2. Verifica al Event Viewer que el servei està actiu
3. Revisa que `runOnStartup: false` si no vols execució immediata

### Errors d'execució

Els errors específics de cada aplicació es registren:
- **MultirIntegraModulab**: Logs a Seq (http://localhost:5341) i Event Viewer
- **MultirRevisioVigencia**: Logs a fitxer i Event Viewer

## Scripts Útils

### Reinstal·lar el servei (PowerShell Administrador)

```powershell
# Aturar i desinstal·lar
Stop-Service -Name "MultirIntegraModulabService" -ErrorAction SilentlyContinue
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u "C:\Projectes\MultirIntegraModulab\MultirIntegraModulab.Service\bin\Release\MultirIntegraModulab.Service.exe"

# Compilar
cd "C:\Projectes\MultirIntegraModulab"
dotnet build MultirIntegraModulab.Service\MultirIntegraModulab.Service.csproj --configuration Release

# Instal·lar i iniciar
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe "C:\Projectes\MultirIntegraModulab\MultirIntegraModulab.Service\bin\Release\MultirIntegraModulab.Service.exe"
Start-Service -Name "MultirIntegraModulabService"
```

## Desenvolupament

### Afegir una nova tasca programada

1. Crea una nova classe a `Jobs/` que implementi `IJob`
2. Afegeix l'entrada al `workflow-schedule.json`
3. Recompila i reinstal·la el servei

### Executar en mode Debug

Per debugar, pots executar els Jobs manualment des del `Main()` del Program.cs o utilitzar el paràmetre `runOnStartup: true` temporalment.

## Autor

Implementació automatitzada amb GitHub Copilot  
Data: 13 de febrer de 2025
