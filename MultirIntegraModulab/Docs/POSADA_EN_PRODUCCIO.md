# 🚀 Guia de Posada en Producció - MultirIntegraModulab

> **Guia completa** per desplegar l'aplicació MultirIntegraModulab en entorn de producció

---

## 📋 Índex

1. [Prerequisits](#-prerequisits)
2. [Preparació del Codi](#-preparació-del-codi)
3. [Configuració de Producció](#-configuració-de-producció)
4. [Compilació i Empaquetatge](#-compilació-i-empaquetatge)
5. [Desplegament al Servidor](#-desplegament-al-servidor)
6. [Verificació Post-Desplegament](#-verificació-post-desplegament)
7. [Monitoratge](#-monitoratge)
8. [Rollback](#-rollback)
9. [Troubleshooting](#-troubleshooting)

---

## ✅ Prerequisits

### En el Servidor de Producció

- [ ] **Windows Server** amb .NET Framework 4.8 instal·lat
- [ ] **Oracle Client** (si no es fa servir Oracle.ManagedDataAccess)
- [ ] **Permisos d'accés** a les bases de dades:
  - Oracle Modulab (excdox-scan.cpd4.intranet.gencat.cat:1522)
  - MySQL MultiR (zeus)
- [ ] **Connectivitat de xarxa** verificada:
  - Accés al WebService SAP: `http://10.80.160.178/flamma/ws/consultaPacient/`
  - Accés al servidor SMTP: `smtp.trueta.intranet`
- [ ] **Carpeta d'instal·lació** creada amb permisos adequats
- [ ] **Usuari de servei Windows** (si es vol configurar com a servei)

### En l'Entorn de Desenvolupament

- [ ] Visual Studio 2019 o superior
- [ ] .NET Framework 4.8 SDK
- [ ] Accés al repositori Git
- [ ] Credencials de producció disponibles

---

## 🔧 Preparació del Codi

### 1. Obtenir l'Última Versió

```bash
# Clonar o actualitzar el repositori
git clone https://github.com/CarlosCastillo70/MultirIntegraModulab
cd MultirIntegraModulab

# O si ja existeix
git fetch origin
git checkout main  # o la branca de release
git pull origin main
```

### 2. Verificar l'Estat del Codi

```bash
# Assegurar-se que estem en la branca correcta
git branch

# Verificar l'últim commit
git log -1

# Comprovar que no hi ha canvis pendents
git status
```

### 3. Revisar els Canvis Recents

```bash
# Veure l'historial de canvis
git log --oneline -10

# Revisar els canvis des de l'última versió desplegada
git diff [TAG_VERSIO_ANTERIOR]..HEAD
```

---

## ⚙️ Configuració de Producció

### 1. Configurar App.config

**⚠️ IMPORTANT:** Aquest és el pas més crític. Revisa cada paràmetre amb atenció.

#### 1.1. Configuració d'Entorn

```xml
<!-- CANVIAR A PRODUCCIÓ -->
<add key="Entorn" value="Produccio" />
```

✅ **Verificar:** El valor ha de ser exactament `"Produccio"`

#### 1.2. Configuració de Càrrega de Dades

```xml
<!-- Dies enrere per carregar resultats -->
<add key="DiesEndarreraCarrega" value="1" />

<!-- NO limitar resultats en producció -->
<add key="LimitResultatsProves" value="0" />

<!-- Processar TOTES les mostres (deixar BUIT) -->
<add key="EtiquetesMostresAProcessar" value="" />
```

✅ **Verificar:**
- `DiesEndarreraCarrega`: 1-2 dies recomanat per evitar sobrecàrrega
- `LimitResultatsProves`: Ha de ser `0` per processar totes les mostres
- `EtiquetesMostresAProcessar`: Ha d'estar **BUIT**

#### 1.3. Connection Strings de Producció

```xml
<connectionStrings>
  <!-- Oracle Modulab - PRODUCCIÓ -->
  <add name="OracleModulab_Produccio"
       connectionString="Data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = excdox-scan.cpd4.intranet.gencat.cat)(PORT = 1522))) (CONNECT_DATA = (SERVICE_NAME = excdox01srv)));User Id=DWGI_MDP;Password=gLesb01an;"
       providerName="Oracle.ManagedDataAccess.Client" />

  <!-- MySQL MultiR - PRODUCCIÓ -->
  <add name="MySqlMultiR_Produccio"
       connectionString="Server=zeus;Database=marsa;Uid=marsa;Pwd=2a0d9a8d22;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

✅ **Verificar:**
- **Oracle:** Host, port, service name, credencials
- **MySQL:** Servidor, **base de dades = `marsa`** (NO `marsa_test`), credencials

#### 1.4. WebService de Producció

```xml
<!-- URL del WebService SAP - Producció -->
<add key="WebServicePacients_Produccio" value="http://10.80.160.178/flamma/ws/consultaPacient/consultaPacient.php" />
```

✅ **Verificar:** IP correcta del servidor de producció (.178, NO .179)

#### 1.5. Configuració de Logging

```xml
<!-- Nivell de logging recomanat per producció -->
<add key="LogLevel" value="Info" />

<!-- Directori de logs -->
<add key="LogDirectory" value="Logs" />
```

✅ **Verificar:**
- `LogLevel`: `"Info"` o `"Warning"` (NO `"Debug"` en producció)
- `LogDirectory`: Assegurar que existeix i té permisos d'escriptura

#### 1.6. Configuració d'Email

```xml
<!-- Configuració SMTP -->
<add key="EnviarEmailLog" value="true" />
<add key="SmtpServer" value="smtp.trueta.intranet" />
<add key="SmtpPort" value="25" />
<add key="SmtpUsarSSL" value="false" />

<!-- Emails -->
<add key="EmailFrom" value="ccastillo.ics@gencat.cat" />
<add key="EmailsDestinataris" value="carloscastillollucia@gmail.com" />

<!-- Rebre sempre resum d'execució -->
<add key="EmailNomesEnErrors" value="false" />
```

✅ **Verificar:**
- Servidor SMTP accessible
- Emails de destinataris correctes (separats per `;` si són múltiples)
- `EmailNomesEnErrors`: `false` per rebre sempre resum

#### 1.7. Altres Configuracions

```xml
<!-- Cache -->
<add key="MinutsVigenciaCache" value="60" />

<!-- Historial -->
<add key="DiesRetencioHistorial" value="90" />

<!-- Processament paral·lel (DESACTIVAT fins validar) -->
<add key="ProcessarMostresEnParalel" value="false" />
```

### 2. Checklist Final de Configuració

- [ ] `Entorn` = `"Produccio"`
- [ ] `LimitResultatsProves` = `0`
- [ ] `EtiquetesMostresAProcessar` = `""` (buit)
- [ ] Connection string Oracle apunta a producció
- [ ] Connection string MySQL usa base de dades `marsa` (NO `marsa_test`)
- [ ] WebService apunta a `.178` (producció)
- [ ] `LogLevel` = `"Info"` o `"Warning"`
- [ ] Emails de destinataris correctes
- [ ] `ProcessarMostresEnParalel` = `false`

---

## 🔨 Compilació i Empaquetatge

### 1. Netejar Solució

```bash
# Dins de Visual Studio: Build > Clean Solution
# O des de línia de comandes:
msbuild MultirIntegraModulab.sln /t:Clean
```

### 2. Restaurar Paquets NuGet

```bash
nuget restore MultirIntegraModulab.sln
```

### 3. Compilar en Mode Release

#### Opció A: Visual Studio

1. Seleccionar configuració `Release` al desplegable superior
2. `Build` > `Rebuild Solution`
3. Verificar que no hi ha errors a la finestra `Error List`

#### Opció B: Línia de Comandes

```bash
msbuild MultirIntegraModulab.sln /p:Configuration=Release /p:Platform="Any CPU"
```

### 4. Verificar la Compilació

```bash
# Comprovar que l'executable s'ha generat correctament
dir bin\Release\MultirIntegraModulab.exe

# Verificar la mida de l'arxiu (ha de ser > 0 bytes)
```

### 5. Preparar el Paquet de Desplegament

Crear una carpeta amb tots els arxius necessaris:

```
MultirIntegraModulab_Release_v[VERSION]\
│
├── MultirIntegraModulab.exe           # Executable principal
├── MultirIntegraModulab.exe.config    # Configuració (verificar!)
├── Oracle.ManagedDataAccess.dll       # Dependencies
├── MySql.Data.dll
├── System.Net.Http.dll
├── [altres DLLs necessàries]
│
├── Logs\                              # Carpeta de logs (buida)
│
└── LEEME_INSTALACIO.txt              # Instruccions ràpides
```

**Script PowerShell per crear el paquet:**

```powershell
# Crear carpeta de release
$version = "1.0.0"  # Canviar segons la versió
$releaseDir = "MultirIntegraModulab_Release_v$version"
New-Item -ItemType Directory -Force -Path $releaseDir

# Copiar fitxers compilats
Copy-Item "bin\Release\*" -Destination $releaseDir -Recurse

# Crear carpeta de logs
New-Item -ItemType Directory -Force -Path "$releaseDir\Logs"

# Comprimir
Compress-Archive -Path $releaseDir -DestinationPath "$releaseDir.zip"
```

### 6. Validacions Pre-Desplegament

- [ ] L'executable existeix i té una mida raonable (> 100 KB)
- [ ] El fitxer `.config` conté la configuració de producció
- [ ] Totes les DLLs necessàries estan presents
- [ ] No hi ha fitxers `.pdb` (debug symbols) en el paquet final
- [ ] No hi ha fitxers de configuració amb credencials de test

---

## 📦 Desplegament al Servidor

### 1. Preparació del Servidor

#### 1.1. Crear Estructura de Directoris

```powershell
# Connectar al servidor de producció (exemple)
# Enter-PSSession -ComputerName [SERVIDOR_PRODUCCIO]

# Crear directori principal
$baseDir = "C:\Apps\MultirIntegraModulab"
New-Item -ItemType Directory -Force -Path $baseDir

# Crear subdirectoris
New-Item -ItemType Directory -Force -Path "$baseDir\Logs"
New-Item -ItemType Directory -Force -Path "$baseDir\Backup"
```

#### 1.2. Backup de la Versió Anterior (si existeix)

```powershell
# Si ja hi ha una versió desplegada
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = "$baseDir\Backup\backup_$timestamp"

if (Test-Path "$baseDir\MultirIntegraModulab.exe") {
    New-Item -ItemType Directory -Force -Path $backupDir
    Copy-Item "$baseDir\*" -Destination $backupDir -Recurse -Exclude "Logs","Backup"
    Write-Host "Backup creat a: $backupDir" -ForegroundColor Green
}
```

### 2. Copiar els Fitxers al Servidor

#### Opció A: Còpia Manual

1. Connectar al servidor via RDP
2. Copiar el ZIP del paquet de release
3. Extreure'l a `C:\Apps\MultirIntegraModulab`

#### Opció B: Còpia Remota (PowerShell)

```powershell
# Des de la màquina local
$local = ".\MultirIntegraModulab_Release_v1.0.0.zip"
$remote = "\\[SERVIDOR]\C$\Apps\MultirIntegraModulab"

Copy-Item $local -Destination $remote

# Descomprimir remotament
Invoke-Command -ComputerName [SERVIDOR] -ScriptBlock {
    Expand-Archive -Path "C:\Apps\MultirIntegraModulab\MultirIntegraModulab_Release_v1.0.0.zip" -DestinationPath "C:\Apps\MultirIntegraModulab" -Force
}
```

### 3. Configurar Permisos

```powershell
# Donar permisos a la carpeta de logs
$acl = Get-Acl "$baseDir\Logs"
$permission = "BUILTIN\Users","FullControl","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl "$baseDir\Logs" $acl
```

### 4. Configurar com a Tasca Programada

#### 4.1. Crear la Tasca

```powershell
# Paràmetres de la tasca
$taskName = "MultirIntegraModulab_Daily"
$taskPath = "\IntegracioModulab\"
$exePath = "C:\Apps\MultirIntegraModulab\MultirIntegraModulab.exe"
$workingDir = "C:\Apps\MultirIntegraModulab"

# Crear acció
$action = New-ScheduledTaskAction -Execute $exePath -WorkingDirectory $workingDir

# Crear trigger (exemple: cada dia a les 02:00 AM)
$trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

# Configuració addicional
$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -RunOnlyIfNetworkAvailable `
    -DontStopIfGoingOnBatteries `
    -AllowStartIfOnBatteries

# Usuari d'execució (canviar segons el vostre entorn)
$principal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" -RunLevel Highest

# Registrar la tasca
Register-ScheduledTask `
    -TaskName $taskName `
    -TaskPath $taskPath `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Description "Integració diària entre MultiR i Modulab"
```

#### 4.2. Verificar la Tasca

```powershell
# Llistar la tasca creada
Get-ScheduledTask -TaskPath $taskPath -TaskName $taskName

# Comprovar l'estat
Get-ScheduledTask -TaskName $taskName | Get-ScheduledTaskInfo
```

### 5. Configurar com a Servei Windows (Alternativa)

Si es prefereix executar com a servei en lloc de tasca programada:

#### 5.1. Instal·lar NSSM (Non-Sucking Service Manager)

```powershell
# Descarregar NSSM des de https://nssm.cc/download
# O instal·lar amb Chocolatey:
choco install nssm
```

#### 5.2. Crear el Servei

```powershell
# Navegar a la carpeta de NSSM
cd "C:\Tools\nssm\win64"

# Instal·lar el servei
.\nssm.exe install MultirIntegraModulab "C:\Apps\MultirIntegraModulab\MultirIntegraModulab.exe"

# Configurar paràmetres
.\nssm.exe set MultirIntegraModulab AppDirectory "C:\Apps\MultirIntegraModulab"
.\nssm.exe set MultirIntegraModulab DisplayName "MultirIntegraModulab Integration Service"
.\nssm.exe set MultirIntegraModulab Description "Servei d'integració entre MultiR i Modulab"
.\nssm.exe set MultirIntegraModulab Start SERVICE_AUTO_START

# Configurar sortida de logs
.\nssm.exe set MultirIntegraModulab AppStdout "C:\Apps\MultirIntegraModulab\Logs\service_stdout.log"
.\nssm.exe set MultirIntegraModulab AppStderr "C:\Apps\MultirIntegraModulab\Logs\service_stderr.log"

# Iniciar el servei
Start-Service MultirIntegraModulab
```

---

## ✔️ Verificació Post-Desplegament

### 1. Test Manual d'Execució

```powershell
# Navegar al directori de l'aplicació
cd C:\Apps\MultirIntegraModulab

# Executar l'aplicació manualment
.\MultirIntegraModulab.exe

# Observar la sortida per consola
# - Ha de connectar a les BD correctament
# - Ha de processar les mostres
# - No ha de mostrar errors crítics
```

### 2. Verificar els Logs

```powershell
# Comprovar que s'han generat logs
Get-ChildItem "C:\Apps\MultirIntegraModulab\Logs" | Sort-Object LastWriteTime -Descending | Select-Object -First 5

# Visualitzar l'últim log
$ultimLog = Get-ChildItem "C:\Apps\MultirIntegraModulab\Logs\*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Get-Content $ultimLog.FullName -Tail 50
```

### 3. Verificar la Connectivitat de BD

Revisar en els logs que:

- [ ] Connexió a Oracle Modulab exitosa
- [ ] Connexió a MySQL MultiR exitosa
- [ ] No hi ha errors de timeout o credencials

### 4. Verificar el WebService

Revisar en els logs que:

- [ ] Crida al WebService de pacients SAP exitosa
- [ ] Respostes del WebService correctes
- [ ] No hi ha errors 404 o 500

### 5. Verificar l'Enviament d'Emails

- [ ] S'ha rebut l'email amb el resum d'execució
- [ ] L'email conté informació coherent (mostres processades, etc.)
- [ ] No hi ha errors d'enviament als logs

### 6. Verificar les Dades a la BD

```sql
-- Connectar a MySQL MultiR i verificar
USE marsa;

-- Comprovar l'última execució
SELECT * FROM historial_execucions 
ORDER BY data_execucio DESC 
LIMIT 1;

-- Comprovar les mostres processades
SELECT * FROM mostres_processades 
WHERE data_processament >= DATE_SUB(NOW(), INTERVAL 1 DAY)
ORDER BY data_processament DESC;
```

### 7. Test de la Tasca Programada

```powershell
# Executar la tasca manualment
Start-ScheduledTask -TaskPath "\IntegracioModulab\" -TaskName "MultirIntegraModulab_Daily"

# Esperar uns minuts i comprovar l'estat
Get-ScheduledTask -TaskName "MultirIntegraModulab_Daily" | Get-ScheduledTaskInfo

# Revisar l'últim resultat
(Get-ScheduledTask -TaskName "MultirIntegraModulab_Daily").LastTaskResult
# Resultat 0 = Èxit
```

### 8. Checklist de Verificació Final

- [ ] Execució manual exitosa sense errors
- [ ] Logs generats correctament amb nivell `Info`
- [ ] Connexions a BD operatives
- [ ] WebService de pacients accessible
- [ ] Email de resum rebut
- [ ] Dades insertades correctament a MultiR
- [ ] Tasca programada configurada i funcional
- [ ] Permisos d'escriptura en carpeta de Logs
- [ ] Backup de versió anterior disponible

---

## 📊 Monitoratge

### 1. Configurar Alertes

#### Alerta per Email en Errors

L'aplicació ja envia emails en cas d'error si està configurat:

```xml
<add key="EnviarEmailLog" value="true" />
<add key="EmailNomesEnErrors" value="false" />
```

#### Script de Monitoratge Automàtic

Crear un script PowerShell per comprovar l'estat:

```powershell
# C:\Apps\MultirIntegraModulab\Scripts\Monitoratge.ps1

$logDir = "C:\Apps\MultirIntegraModulab\Logs"
$alertEmail = "admin@example.com"
$smtpServer = "smtp.trueta.intranet"

# Obtenir l'últim log del dia
$avui = Get-Date -Format "yyyyMMdd"
$logFile = Get-ChildItem "$logDir\*$avui*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($logFile) {
    # Buscar errors en el log
    $errors = Select-String -Path $logFile.FullName -Pattern "ERROR|CRITICAL|FATAL"
    
    if ($errors.Count -gt 0) {
        # Enviar alerta
        $subject = "[ALERT] MultirIntegraModulab - Errors detectats"
        $body = "S'han detectat $($errors.Count) errors en l'execució d'avui.`n`n"
        $body += $errors | Out-String
        
        Send-MailMessage -From "alerts@system.com" -To $alertEmail -Subject $subject -Body $body -SmtpServer $smtpServer
    }
} else {
    # No s'ha generat log avui - possiblement no s'ha executat
    $subject = "[ALERT] MultirIntegraModulab - No s'ha executat avui"
    $body = "No s'ha trobat cap log d'execució per a la data: $avui"
    
    Send-MailMessage -From "alerts@system.com" -To $alertEmail -Subject $subject -Body $body -SmtpServer $smtpServer
}
```

Afegir aquest script com a tasca programada diària.

### 2. Dashboard de Monitoratge (Opcional)

Crear consultes SQL per obtenir mètriques:

```sql
-- Mostres processades per dia
SELECT 
    DATE(data_processament) as data,
    COUNT(*) as total_mostres,
    SUM(CASE WHEN tipus = 'POSITIVA' THEN 1 ELSE 0 END) as positives,
    SUM(CASE WHEN tipus = 'NEGATIVA' THEN 1 ELSE 0 END) as negatives,
    SUM(CASE WHEN error IS NOT NULL THEN 1 ELSE 0 END) as amb_errors
FROM mostres_processades
WHERE data_processament >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY DATE(data_processament)
ORDER BY data DESC;

-- Temps d'execució per dia
SELECT 
    data_execucio,
    temps_execucio_segons,
    mostres_processades,
    errors_detectats
FROM historial_execucions
WHERE data_execucio >= DATE_SUB(NOW(), INTERVAL 30 DAY)
ORDER BY data_execucio DESC;
```

### 3. Indicadors Clau (KPIs)

Monitorar regularment:

| KPI | Valor Esperat | Acció si es Desvia |
|-----|---------------|---------------------|
| Temps d'execució | < 10 minuts | Investigar rendiment |
| Mostres processades/dia | 50-500 | Verificar càrrega |
| Taxa d'errors | < 5% | Revisar logs i configuració |
| Disponibilitat tasca | 100% | Verificar scheduler |
| Mida logs | < 100 MB/dia | Ajustar nivell logging |

---

## 🔄 Rollback

Si es detecten problemes després del desplegament:

### 1. Rollback Ràpid

```powershell
# Aturar la tasca programada
Disable-ScheduledTask -TaskPath "\IntegracioModulab\" -TaskName "MultirIntegraModulab_Daily"

# O aturar el servei
Stop-Service MultirIntegraModulab

# Restaurar la versió anterior des del backup
$backupDir = "C:\Apps\MultirIntegraModulab\Backup\backup_[TIMESTAMP]"
$prodDir = "C:\Apps\MultirIntegraModulab"

# Eliminar versió actual (mantenir logs)
Remove-Item "$prodDir\*.exe", "$prodDir\*.dll", "$prodDir\*.config" -Force

# Restaurar des del backup
Copy-Item "$backupDir\*" -Destination $prodDir -Recurse -Force -Exclude "Logs"

# Reactivar la tasca/servei
Enable-ScheduledTask -TaskPath "\IntegracioModulab\" -TaskName "MultirIntegraModulab_Daily"
# o
Start-Service MultirIntegraModulab
```

### 2. Verificar el Rollback

```powershell
# Executar manualment per verificar
cd C:\Apps\MultirIntegraModulab
.\MultirIntegraModulab.exe

# Comprovar versió (si està implementat en el codi)
.\MultirIntegraModulab.exe --version
```

### 3. Documentar l'Incident

Crear un informe amb:
- Data i hora del problema
- Símptomes detectats
- Versió problemàtica
- Versió de rollback
- Accions realitzades
- Temps de downtime

---

## 🔍 Troubleshooting

### Error: "No es pot connectar a Oracle"

**Símptomes:** Errors de connexió a la BD Oracle als logs

**Solucions:**

1. Verificar connectivitat de xarxa:
   ```powershell
   Test-NetConnection -ComputerName excdox-scan.cpd4.intranet.gencat.cat -Port 1522
   ```

2. Comprovar credencials en `App.config`

3. Verificar l'estat del servei Oracle:
   ```sql
   SELECT * FROM v$instance;
   ```

4. Revisar el TNS_ADMIN si s'utilitza (normalment no amb ManagedDataAccess)

### Error: "No es pot connectar a MySQL"

**Símptomes:** Errors de connexió a MySQL als logs

**Solucions:**

1. Verificar connectivitat:
   ```powershell
   Test-NetConnection -ComputerName zeus -Port 3306
   ```

2. Provar connexió amb MySQL Workbench o línia de comandes:
   ```bash
   mysql -h zeus -u marsa -p marsa
   ```

3. Verificar que s'està usant la BD correcta (`marsa`, no `marsa_test`)

### Error: "Timeout al WebService de pacients"

**Símptomes:** Errors de timeout o 404 al WebService SAP

**Solucions:**

1. Verificar URL:
   ```powershell
   Invoke-WebRequest -Uri "http://10.80.160.178/flamma/ws/consultaPacient/consultaPacient.php"
   ```

2. Augmentar el timeout a `App.config`:
   ```xml
   <add key="WebServiceTimeout" value="60" />
   ```

3. Contactar amb l'equip de SAP per verificar disponibilitat

### Error: "No s'envien els emails"

**Símptomes:** No es reben emails de resum

**Solucions:**

1. Verificar connectivitat SMTP:
   ```powershell
   Test-NetConnection -ComputerName smtp.trueta.intranet -Port 25
   ```

2. Provar enviar email manualment:
   ```powershell
   Send-MailMessage -From "test@test.com" -To "admin@test.com" -Subject "Test" -Body "Test" -SmtpServer "smtp.trueta.intranet"
   ```

3. Revisar configuració SMTP a `App.config`

4. Comprovar els logs per errors d'enviament

### Error: "La tasca programada no s'executa"

**Símptomes:** La tasca no s'inicia automàticament

**Solucions:**

1. Verificar l'estat:
   ```powershell
   Get-ScheduledTask -TaskName "MultirIntegraModulab_Daily"
   ```

2. Comprovar l'historial:
   ```powershell
   Get-ScheduledTask -TaskName "MultirIntegraModulab_Daily" | Get-ScheduledTaskInfo
   ```

3. Revisar el Event Viewer de Windows:
   - Windows Logs > Application
   - Windows Logs > Task Scheduler

4. Executar manualment per veure errors:
   ```powershell
   Start-ScheduledTask -TaskName "MultirIntegraModulab_Daily"
   ```

### Error: "Accés denegat a la carpeta Logs"

**Símptomes:** No es generen fitxers de log

**Solucions:**

1. Verificar permisos:
   ```powershell
   Get-Acl "C:\Apps\MultirIntegraModulab\Logs"
   ```

2. Donar permisos d'escriptura:
   ```powershell
   $acl = Get-Acl "C:\Apps\MultirIntegraModulab\Logs"
   $permission = "BUILTIN\Users","FullControl","ContainerInherit,ObjectInherit","None","Allow"
   $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
   $acl.SetAccessRule($accessRule)
   Set-Acl "C:\Apps\MultirIntegraModulab\Logs" $acl
   ```

3. Executar la tasca/servei amb un usuari amb permisos adequats

### Problemes de Rendiment

**Símptomes:** L'execució triga massa temps

**Solucions:**

1. Activar logging detallat temporalment:
   ```xml
   <add key="LogLevel" value="Debug" />
   ```

2. Analitzar els logs per identificar colls d'ampolla

3. Considerar activar processament paral·lel (EXPERIMENTAL):
   ```xml
   <add key="ProcessarMostresEnParalel" value="true" />
   <add key="MaxGrauParalelisme" value="4" />
   ```

4. Reduir `DiesEndarreraCarrega` si es processen massa mostres:
   ```xml
   <add key="DiesEndarreraCarrega" value="1" />
   ```

5. Optimitzar consultes SQL (revisar plans d'execució)

---

## 📞 Contactes i Suport

### Equip de Desenvolupament

- **Desenvolupador Principal:** [Nom]
- **Email:** carloscastillollucia@gmail.com
- **Repositori:** https://github.com/CarlosCastillo70/MultirIntegraModulab

### Equip d'Infraestructura

- **Responsable Servidors:** [Nom]
- **Email:** [email]

### Bases de Dades

- **DBA Oracle:** [Nom]
- **DBA MySQL:** [Nom]

### Escalat d'Incidències

1. **Nivell 1:** Consultar aquesta documentació i els logs
2. **Nivell 2:** Contactar amb el desenvolupador principal
3. **Nivell 3:** Involucrar equip d'infraestructura o DBAs segons sigui necessari

---

## 📚 Referències

- [Documentació Principal](README.md)
- [Clean Architecture](CLEAN_ARCHITECTURE_README.md)
- [Configuració](../Configuration/README.md)
- [Historial de Canvis](README_HISTORIAL.md)
- [Repositori GitHub](https://github.com/CarlosCastillo70/MultirIntegraModulab)

---

## 📝 Historial de Versions d'aquest Document

| Versió | Data | Autor | Canvis |
|--------|------|-------|--------|
| 1.0 | 2024-XX-XX | [Nom] | Creació inicial |

---

## ✅ Checklist Final de Desplegament

Abans de donar per finalitzat el desplegament, assegurar-se que:

- [ ] Backup de la versió anterior realitzat
- [ ] Codi compilat en mode Release sense errors
- [ ] `App.config` revisat amb configuració de producció
- [ ] Connection strings apunten a les BD de producció
- [ ] Fitxers copiats al servidor correctament
- [ ] Permisos de carpetes configurats
- [ ] Tasca programada o servei creat i configurat
- [ ] Execució manual exitosa
- [ ] Logs generats correctament
- [ ] Connexions a BD verificades
- [ ] WebService accessible
- [ ] Email de resum rebut
- [ ] Dades insertades correctament a MultiR
- [ ] Monitoratge configurat
- [ ] Documentació actualitzada
- [ ] Equip notificat del desplegament

---

**✨ Desplegament completat amb èxit! ✨**

En cas de dubtes o problemes, consultar la secció de [Troubleshooting](#-troubleshooting) o contactar amb l'equip de desenvolupament.
