# ✅ WINDOWS SERVICE - LLEST PER PRODUCCIÓ

## MultirIntegraModulab Service v1.0.0

**Data**: 25/01/2026  
**Estat**: ✅ **APTE PER PRODUCCIÓ**

---

## 📊 RESUM EXECUTIU

El Windows Service ha estat **revisat i corregit**. Tots els problemes crítics han estat resolts i està llest per desplegar a producció.

### ✅ Correccions Aplicades

1. **Event Log Sources**: Ara es creen durant la instal·lació (no dinàmicament)
2. **Timeouts**: Afegit timeout de 30 minuts per evitar bloquejos
3. **Captura d'Outputs**: STDERR es captura i es logga per debugging
4. **Gestió d'Errors**: Millor control d'excepcions

---

## 🎯 FUNCIONALITATS

### Tasca 1: Processar Mostres Modulab
- **Freqüència**: Cada 15 minuts
- **CRON**: `0 0/15 * * * ?`
- **Executable**: `MultirIntegraModulab.exe`
- **Timeout**: 30 minuts
- **Log Source**: `MultirIntegraModulabService`

### Tasca 2: Revisar Vigència Diagnòstics
- **Freqüència**: Cada dia a les 4:00 AM
- **CRON**: `0 0 4 * * ?`
- **Executable**: `MultirRevisioVigencia.exe`
- **Timeout**: 30 minuts
- **Log Source**: `MultirRevisioVigenciaService`

---

## 🚀 DESPLEGAMENT RÀPID

### Passos d'Instal·lació

1. **Compilar en Release**
   ```powershell
   cd C:\Projectes\MultirIntegraModulab
   dotnet build MultirIntegraModulab.Service -c Release
   # o des de Visual Studio: Build > Build Solution (Release)
   ```

2. **Copiar fitxers al servidor**
   - Tot el contingut de `bin\Release\` del projecte Service
   - `MultirIntegraModulab.exe` (al mateix directori)
   - `MultirRevisioVigencia.exe` (al mateix directori)

3. **Instal·lar servei** (com a Administrador)
   ```powershell
   cd C:\Path\To\Service
   .\Install-Service.ps1
   ```

4. **Verificar**
   ```powershell
   Get-Service MultirIntegraModulabService
   # Hauria de mostrar: Status=Running
   ```

---

## 📁 FITXERS IMPORTANTS

### Documentació Creada
- ✅ `DEPLOYMENT-README.md` - Guia completa de desplegament
- ✅ `PRE-PRODUCTION-CHECKLIST.md` - Checklist de validació
- ✅ `PRE-PRODUCTION-REVIEW.md` - Informe de revisió detallat
- ✅ `PRODUCTION-READY-SUMMARY.md` - Aquest document

### Scripts
- ✅ `Install-Service.ps1` - Script d'instal·lació automatitzat
- ✅ `Uninstall-Service.ps1` - Script de desinstal·lació

### Codi Revisat i Corregit
- ✅ `Jobs\ProcessarMostresModulabJob.cs`
- ✅ `Jobs\RevisarVigenciaDiagnosticsJob.cs`
- ✅ `Program.cs`
- ✅ `Services\WorkflowService.cs`

---

## ⚠️ CHECKLIST FINAL

Abans de desplegar, assegura't de:

- [ ] ✅ Build compilat en mode **Release**
- [ ] ✅ `MultirIntegraModulab.exe` present al directori
- [ ] ✅ `MultirIntegraModulab.exe.config` present (App.config copiat automàticament)
- [ ] ✅ `MultirRevisioVigencia.exe` present al directori
- [ ] ✅ `MultirRevisioVigencia.exe.config` present (App.config copiat automàticament)
- [ ] ✅ `workflow-schedule.json` present i vàlid
- [ ] ✅ Totes les DLLs copiades (Quartz, Newtonsoft.Json, etc.)
- [ ] ✅ Script `Install-Service.ps1` executable

**💡 TIP**: Executa `.\Verify-Build.ps1` per verificar tots els fitxers automàticament
- [ ] ✅ PowerShell executat com a **Administrador**
- [ ] ✅ .NET Framework 4.8 instal·lat al servidor

---

## 📝 CONFIGURACIÓ ACTUAL

### workflow-schedule.json
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

**Per canviar freqüències**: Editar el camp `cron` i reiniciar el servei.

### App.config dels Executables

**IMPORTANT**: Cada executable té el seu propi fitxer de configuració:

#### MultirIntegraModulab.exe.config
- Controla l'entorn (Producció/Preproducció)
- Configuració de càrrega de dades (Incremental/Dies enrere/Rang dates)
- Connection strings a MySQL i Oracle
- **Editar aquest fitxer** al servidor per canviar paràmetres

#### MultirRevisioVigencia.exe.config
- Controla l'entorn (Producció/Preproducció)
- Límit de diagnòstics a processar
- Configuració de logging (Serilog, Seq)
- **Editar aquest fitxer** al servidor per canviar paràmetres

**📖 Més info**: Veure `APP-CONFIG-EXPLICACIO.md` per detalls sobre com funcionen els App.config

---

## 📊 MONITORITZACIÓ

### Event Viewer
**Path**: Windows + R → `eventvwr.msc` → Application

**Sources**:
- `MultirIntegraModulabService` - Servei i processament Modulab
- `MultirRevisioVigenciaService` - Revisió vigència

### Logs Esperats

**Cada 15 minuts** (Modulab):
```
[25/01/2026 10:15:00] Iniciant processament de mostres Modulab...
Processament finalitzat. Durada: XX.XXs. Exit code: 0
```

**Cada dia a les 4:00 AM** (Vigència):
```
[25/01/2026 04:00:00] Iniciant revisió de vigència de diagnòstics...
Revisió finalitzada. Durada: XX.XXs. Exit code: 0
```

**En cas d'error**:
```
ERROR: [missatge d'error]
STDERR del procés: [detalls si n'hi ha]
```

**En cas de timeout**:
```
TIMEOUT: El processament ha excedit els 30 minuts i s'ha finalitzat forçadament
```

---

## 🛠️ GESTIÓ DEL SERVEI

### Comandos PowerShell
```powershell
# Estat del servei
Get-Service MultirIntegraModulabService

# Iniciar
Start-Service MultirIntegraModulabService

# Aturar
Stop-Service MultirIntegraModulabService

# Reiniciar (després de canviar configuració)
Restart-Service MultirIntegraModulabService
```

### Services.msc
Windows + R → `services.msc` → Buscar "MultiR Integra Modulab Service"

---

## 🔧 MODIFICACIONS COMUNES

### Canviar freqüència d'execució

1. Editar `workflow-schedule.json`
2. Modificar el camp `cron`
3. Reiniciar servei: `Restart-Service MultirIntegraModulabService`

### Exemples de CRON:
```
"0 0/5 * * * ?"   → Cada 5 minuts
"0 0/30 * * * ?"  → Cada 30 minuts
"0 0 */2 * * ?"   → Cada 2 hores
"0 30 8 * * ?"    → Cada dia a les 8:30 AM
"0 0 12 ? * MON"  → Cada dilluns a les 12:00 PM
```

---

## 🚨 RESOLUCIÓ DE PROBLEMES

### El servei no inicia
1. Verificar logs Event Viewer
2. Comprovar que tots els fitxers existeixen
3. Verificar permisos del directori
4. Reinstal·lar: `.\Uninstall-Service.ps1` i després `.\Install-Service.ps1`

### Els executables no es troben
```
ERROR: No s'ha trobat l'executable: C:\...\MultirIntegraModulab.exe
```
**Solució**: Copiar `MultirIntegraModulab.exe` i `MultirRevisioVigencia.exe` al directori del servei.

### Timeout
```
TIMEOUT: El processament ha excedit els 30 minuts
```
**Causa**: L'executable està trigant més de 30 minuts.  
**Solució**: Optimitzar el processament o augmentar timeout al codi.

---

## 📞 SUPORT

**Repositori**: https://github.com/CarlosCastillo70/MultirIntegraModulab  
**Branch Producció**: `main` o `master`  
**Branch Desenvolupament**: `developer`

---

## ✅ VALIDACIÓ FINAL

### Build Status
✅ **Compilació exitosa** (sense warnings crítics)

### Tests Realitzats
✅ Servei instal·la correctament  
✅ Tasques es programen amb Quartz  
✅ Timeouts funcionen  
✅ Logs es generen a Event Viewer  
✅ Event Sources es creen durant instal·lació  

### Code Quality
✅ Event Sources no es creen dinàmicament  
✅ Timeouts de 30 minuts implementats  
✅ STDERR capturada i loggada  
✅ Gestió d'errors robusta  

---

## 🎉 CONCLUSIÓ

El **MultirIntegraModulab Windows Service** està **llest per producció**. 

Totes les correccions crítiques han estat aplicades i testejades. El servei és robust, té bona gestió d'errors, i està ben documentat.

**Recomanació**: Fer un desplegament en un entorn de pre-producció durant 24-48h abans del desplegament final per validar el comportament en condicions reals.

---

**Preparat per**: GitHub Copilot  
**Revisat**: ✅  
**Build**: ✅  
**Tests**: ✅  
**Documentació**: ✅  
**Estat Final**: 🚀 **READY TO DEPLOY**
