# ✅ RESPOSTA RÀPIDA: App.config després de Compilar

## ❓ Pregunta
> "Si compilem els projectes, l'App.config ja no es té més en compte?"

## ✅ RESPOSTA CURTA

**NO, SÍ que es té en compte!** 

L'`App.config` es **copia automàticament** durant la compilació amb el nom `[Executable].exe.config`.

---

## 📋 QUÈ PASSA DURANT LA COMPILACIÓ

```
ABANS:                          DESPRÉS (bin\Release\):
──────                          ─────────────────────────

App.config                      [Executable].exe
								[Executable].exe.config  ← Còpia de App.config
```

---

## 📁 EXEMPLE REAL

### 1️⃣ MultirIntegraModulab

```
COMPILAR:
  MultirIntegraModulab\App.config

RESULTAT (bin\Release\):
  ✅ MultirIntegraModulab.exe
  ✅ MultirIntegraModulab.exe.config  ← Aquí està la configuració
```

### 2️⃣ MultirRevisioVigencia

```
COMPILAR:
  MultirRevisioVigencia\App.config

RESULTAT (bin\Release\):
  ✅ MultirRevisioVigencia.exe
  ✅ MultirRevisioVigencia.exe.config  ← Aquí està la configuració
```

### 3️⃣ Windows Service

```
COMPILAR:
  MultirIntegraModulab.Service\App.config

RESULTAT (bin\Release\):
  ✅ MultirIntegraModulab.Service.exe
  ✅ MultirIntegraModulab.Service.exe.config  ← Aquí està la configuració
```

---

## 🔍 COM HO VERIFICA .NET?

Quan executes un programa:

```
MultirIntegraModulab.exe
  │
  └─> Busca: MultirIntegraModulab.exe.config (al mateix directori)
	  │
	  └─> Llegeix: <appSettings>, <connectionStrings>, etc.
```

---

## ⚠️ IMPORTANT AL DESPLEGAR

### ✅ Estructura Correcta al Servidor

```
C:\Program Files\MultirIntegraModulabService\
├── MultirIntegraModulab.Service.exe
├── MultirIntegraModulab.Service.exe.config       ← Config del servei
│
├── MultirIntegraModulab.exe
├── MultirIntegraModulab.exe.config               ← Config de Modulab ⚠️
│
├── MultirRevisioVigencia.exe
├── MultirRevisioVigencia.exe.config              ← Config de Revisió ⚠️
│
└── workflow-schedule.json
```

**CADA executable necessita el seu `.exe.config` al costat!**

---

## 🧪 COM VERIFICAR?

### Opció 1: Manual

Després de compilar en Release, verifica:

```powershell
dir "MultirIntegraModulab\bin\Release\MultirIntegraModulab.exe.config"
dir "MultirRevisioVigencia\bin\Release\MultirRevisioVigencia.exe.config"
dir "MultirIntegraModulab.Service\bin\Release\MultirIntegraModulab.Service.exe.config"
```

Si tots existeixen → ✅ Correcte!

### Opció 2: Script Automàtic

```powershell
.\Verify-Build.ps1
```

---

## 🔧 MODIFICAR CONFIGURACIÓ EN PRODUCCIÓ

### ❌ MAL (no funciona)

```powershell
# Editar App.config al codi font
notepad "MultirIntegraModulab\App.config"

# Això NO té efecte si no recompiles!
```

### ✅ BÉ (funciona)

```powershell
# Editar .exe.config al servidor
notepad "C:\Program Files\...\MultirIntegraModulab.exe.config"

# Canviar per exemple:
<add key="Entorn" value="Preproduccio" />
# a:
<add key="Entorn" value="Produccio" />

# Guardar i reiniciar el servei (si cal)
Restart-Service MultirIntegraModulabService
```

---

## 📖 DOCUMENTACIÓ COMPLETA

Per més detalls, consulta:

- **`APP-CONFIG-EXPLICACIO.md`** - Explicació completa i exemples
- **`PRODUCTION-READY-SUMMARY.md`** - Guia de desplegament
- **`Verify-Build.ps1`** - Script de verificació automàtica

---

## ✅ CONCLUSIÓ

```
╔════════════════════════════════════════════════╗
║                                                ║
║  ✅ SÍ, App.config es té en compte            ║
║                                                ║
║  📋 Es copia com [Executable].exe.config      ║
║                                                ║
║  ⚠️  Cal copiar els .exe.config al servidor   ║
║                                                ║
╚════════════════════════════════════════════════╝
```

**Els teus 3 projectes estan correctament configurats! ✅**

---

**Data**: 25/01/2026  
**Verificat**: Tots els App.config es copien correctament
