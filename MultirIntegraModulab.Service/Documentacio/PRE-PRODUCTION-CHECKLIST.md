# ✅ CHECKLIST DE VALIDACIÓ PRE-PRODUCCIÓ
# MultirIntegraModulab Windows Service

## 📦 1. COMPILACIÓ I FITXERS

### Build Release
- [ ] Projecte compilat en mode **Release** (no Debug)
- [ ] Build exitós sense warnings crítics
- [ ] Tests executats i passats

### Fitxers Obligatoris
- [ ] `MultirIntegraModulab.Service.exe` (executable del servei)
- [ ] `MultirIntegraModulab.exe` (executable integració Modulab)
- [ ] `MultirRevisioVigencia.exe` (executable revisió vigència)
- [ ] `workflow-schedule.json` (configuració tasques)
- [ ] `App.config` / `MultirIntegraModulab.Service.exe.config`

### Dependències DLL
- [ ] `Quartz.dll`
- [ ] `Newtonsoft.Json.dll`
- [ ] `Microsoft.Extensions.Logging.Abstractions.dll`
- [ ] Altres DLLs referenciades

---

## ⚙️ 2. CONFIGURACIÓ

### workflow-schedule.json
- [ ] Fitxer present i vàlid (JSON vàlid)
- [ ] Expressió CRON Modulab: `0 0/15 * * * ?` (cada 15 minuts)
- [ ] Expressió CRON Vigència: `0 0 4 * * ?` (cada dia 4:00 AM)
- [ ] Tipus i Assembly correctes per cada job
- [ ] `runOnStartup` configurat segons necessitats

### App.config
- [ ] ServiceName: `MultirIntegraModulabService`
- [ ] ConnectionStrings configurats (si aplica)
- [ ] Configuracions específiques d'entorn (producció)

---

## 🔐 3. PERMISOS I SEGURETAT

### Permisos del Directori
- [ ] El directori té permisos de lectura per al servei
- [ ] El directori té permisos d'escriptura si genera logs locals
- [ ] Els executables tenen permisos d'execució

### Event Log
- [ ] Compte del servei té permisos per escriure a Event Log
- [ ] Event Sources poden ser creats (requereix admin la primera vegada)

---

## 🧪 4. VALIDACIÓ FUNCIONAL

### Test Manual
- [ ] Executar `MultirIntegraModulab.exe` manualment → Funciona OK
- [ ] Executar `MultirRevisioVigencia.exe` manualment → Funciona OK
- [ ] Verificar que generen logs correctes
- [ ] Verificar que no deixen processos penjats

### Test del Servei (en entorn de test)
- [ ] Instal·lar servei amb script `Install-Service.ps1`
- [ ] Servei inicia correctament
- [ ] Logs apareixen al Event Viewer
- [ ] Esperar un cicle d'execució (15 min per Modulab)
- [ ] Verificar que les tasques s'executen automàticament
- [ ] Aturar i reiniciar servei → Funciona OK
- [ ] Desinstal·lar amb `Uninstall-Service.ps1` → OK

---

## 📊 5. MONITORITZACIÓ

### Event Viewer
- [ ] Event Source `MultirIntegraModulabService` es crea
- [ ] Event Source `MultirRevisioVigenciaService` es crea
- [ ] Logs de tipus Information, Warning i Error es generen
- [ ] Format de logs és llegible i útil

### Verificacions de Logs
- [ ] Logs mostren hora d'inici d'execució
- [ ] Logs mostren durada d'execució
- [ ] Logs mostren exit code del procés
- [ ] Logs d'error inclouen stack trace

---

## 🔄 6. RECUPERACIÓ I RESILIÈNCIA

### Configuració del Servei
- [ ] Inici automàtic (Automatic Start)
- [ ] Recuperació automàtica configurada (3 intents)
- [ ] Delay de 60s entre reinicis
- [ ] Servei pot ser aturat i reiniciat sense problemes

### Gestió d'Errors
- [ ] Jobs tenen `[DisallowConcurrentExecution]`
- [ ] Errors capturats i loggats correctament
- [ ] No es queden processos zombi
- [ ] Timeout considerat si l'executable no respon

---

## 🚀 7. DESPLEGAMENT

### Pre-Desplegament
- [ ] Backup de la configuració actual (si n'hi ha)
- [ ] Planificar finestra de manteniment si cal
- [ ] Documentar path d'instal·lació: `__________________`
- [ ] Usuari del servei decidit (Local System / específic)

### Instal·lació
- [ ] Copiar tots els fitxers al servidor de producció
- [ ] Executar `Install-Service.ps1` com a Administrador
- [ ] Verificar que el servei apareix a `services.msc`
- [ ] Iniciar el servei
- [ ] Verificar estat: `Get-Service MultirIntegraModulabService`

### Post-Desplegament
- [ ] Revisar Event Viewer els primers 30 minuts
- [ ] Verificar primera execució de Modulab (15 min)
- [ ] Verificar execució de Vigència a les 4:00 AM l'endemà
- [ ] Documentar qualsevol incidència
- [ ] Informar a l'equip que el servei està actiu

---

## 📝 8. DOCUMENTACIÓ

- [ ] README de desplegament revisat
- [ ] Scripts d'instal·lació/desinstal·lació disponibles
- [ ] Contacte de suport documentat
- [ ] Procediment de rollback definit
- [ ] Logs i monitorització explicats a l'equip

---

## 🎯 VERIFICACIÓ FINAL

### Criteris d'Acceptació
- [ ] ✅ Servei instal·lat i en execució
- [ ] ✅ Tasques s'executen segons el schedule
- [ ] ✅ Logs generats correctament
- [ ] ✅ No hi ha errors crítics al Event Viewer
- [ ] ✅ Servei sobreviu a reinicis del servidor
- [ ] ✅ Equip format en monitorització i operació

---

## 📞 INFORMACIÓ DE CONTACTE

**Path Instal·lació**: _____________________________  
**Data Desplegament**: _____________________________  
**Desplegat per**: _____________________________  
**Versió**: 1.0.0

---

## ⚠️ ROLLBACK

En cas de problemes:

1. Aturar servei:
   ```powershell
   Stop-Service MultirIntegraModulabService
   ```

2. Desinstal·lar:
   ```powershell
   .\Uninstall-Service.ps1
   ```

3. Restaurar configuració anterior si n'hi havia

---

**IMPORTANT**: No desplegar a producció fins que TOTS els checkboxes estiguin marcats!
