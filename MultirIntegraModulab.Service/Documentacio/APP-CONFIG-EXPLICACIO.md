# 📝 App.config i Compilació - Explicació Detallada

## ❓ Pregunta Original

> "Cada un dels projectes a executar tenen un App.config per configurar paràmetres d'execució. Si els compilem els projectes, aquest App.config ja no es té més en compte?"

## ✅ RESPOSTA: SÍ, ES TÉ EN COMPTE

L'`App.config` **SÍ que es té en compte** després de compilar. El que passa és això:

---

## 🔄 Procés de Compilació

### 1. Durant la Compilació

Quan compiles un projecte:

```
App.config  →  [MSBuild]  →  [NomExecutable].exe.config
							   (a bin\Debug o bin\Release)
```

### 2. Exemple Concret

```
ABANS DE COMPILAR:
MultirIntegraModulab/
└── App.config

DESPRÉS DE COMPILAR (bin\Release\):
MultirIntegraModulab/
├── App.config                         ← Original (no es toca)
└── bin/Release/
	├── MultirIntegraModulab.exe
	└── MultirIntegraModulab.exe.config  ← CÒPIA automàtica
```

### 3. El .NET Framework llegeix això

Quan executes `MultirIntegraModulab.exe`, el framework busca:
```
MultirIntegraModulab.exe.config
```

I **aquest fitxer** és el que conté la configuració.

---

## 📁 Estructura Completa al Servidor

Quan desplegues el Windows Service amb els 3 projectes:

```
C:\Program Files\MultirIntegraModulabService\
├── MultirIntegraModulab.Service.exe
├── MultirIntegraModulab.Service.exe.config       ← Config del servei
│
├── MultirIntegraModulab.exe                      ← Executable Modulab
├── MultirIntegraModulab.exe.config               ← Config de Modulab
│
├── MultirRevisioVigencia.exe                     ← Executable Revisió
├── MultirRevisioVigencia.exe.config              ← Config de Revisió
│
├── Quartz.dll
├── Newtonsoft.Json.dll
├── MySql.Data.dll
└── workflow-schedule.json                        ← Config del scheduler
```

**Cada executable llegeix el seu propi `.exe.config`**

---

## ⚙️ Com Funciona el .csproj

### MultirIntegraModulab.csproj (SDK Style)

Aquest projecte usa l'estil **SDK-style** (més modern):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
	<OutputType>Exe</OutputType>
	<TargetFramework>net48</TargetFramework>
  </PropertyGroup>
</Project>
```

**Per defecte**, l'App.config es copia automàticament sense necessitat d'especificar-ho.

### MultirRevisioVigencia.csproj (Old Style)

Aquest projecte usa l'estil **classic**:

```xml
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
	<None Include="App.config" />
  </ItemGroup>
</Project>
```

L'`App.config` està declarat amb `<None Include="App.config" />`, que significa:
- Es copia a la sortida
- No es compila (és un fitxer de dades)

**També es copia automàticament.**

---

## ✅ Verificació: App.config es copien correctament?

### Verificació Automàtica

Ambdós projectes tenen l'`App.config` correctament configurat:

#### ✅ MultirIntegraModulab
- **Tipus**: SDK-style (.NET 4.8)
- **App.config**: Present i es copiarà automàticament
- **Ubicació**: `MultirIntegraModulab\App.config`

#### ✅ MultirRevisioVigencia
- **Tipus**: Classic-style (.NET Framework 4.8)
- **App.config**: Present a `<None Include="App.config" />`
- **Ubicació**: `MultirRevisioVigencia\App.config`

#### ✅ MultirIntegraModulab.Service
- **Tipus**: Classic-style (.NET Framework 4.8)
- **App.config**: Present
- **Ubicació**: `MultirIntegraModulab.Service\App.config`

**CONCLUSIÓ**: Tots els projectes tenen els seus App.config correctament configurats per copiar-se.

---

## 🧪 Com Verificar Després de Compilar

### 1. Compilar en Release

```powershell
# Des de Visual Studio
Build > Configuration Manager > Release > Build Solution

# O des de la línia de comandes
msbuild MultirIntegraModulab.sln /p:Configuration=Release
```

### 2. Verificar que els .config existeixen

```powershell
# MultirIntegraModulab
dir "MultirIntegraModulab\bin\Release\MultirIntegraModulab.exe.config"

# MultirRevisioVigencia
dir "MultirRevisioVigencia\bin\Release\MultirRevisioVigencia.exe.config"

# Service
dir "MultirIntegraModulab.Service\bin\Release\MultirIntegraModulab.Service.exe.config"
```

Si tots existeixen → ✅ Tot correcte

---

## 🚨 Problemes Comuns i Solucions

### Problema 1: El .exe.config no es crea

**Causa**: L'App.config no està inclòs al projecte

**Solució**:
1. Verificar que `App.config` apareix a l'explorador de solucions
2. Propietats del fitxer:
   - **Build Action**: `None`
   - **Copy to Output Directory**: `Do not copy` (es copia automàticament amb altre nom)

### Problema 2: La configuració no s'aplica

**Causa**: El fitxer `.exe.config` no està al mateix directori que l'`.exe`

**Solució**: Assegurar-se que tots dos fitxers estan junts:
```
bin\Release\
├── MultirIntegraModulab.exe
└── MultirIntegraModulab.exe.config  ← Ha d'estar aquí
```

### Problema 3: Canvis a App.config no tenen efecte

**Causa**: Has modificat l'`App.config` original però no has recompilat

**Solució**: 
1. Modificar `App.config`
2. **Rebuild** el projecte
3. El nou `.exe.config` es generarà amb els canvis

---

## 📋 Checklist de Desplegament

Quan despleguis al servidor, has de copiar:

### Per a MultirIntegraModulab:
- [ ] `MultirIntegraModulab.exe`
- [ ] `MultirIntegraModulab.exe.config` ← **IMPORTANT**
- [ ] Totes les DLLs necessàries

### Per a MultirRevisioVigencia:
- [ ] `MultirRevisioVigencia.exe`
- [ ] `MultirRevisioVigencia.exe.config` ← **IMPORTANT**
- [ ] Totes les DLLs necessàries

### Per al Windows Service:
- [ ] `MultirIntegraModulab.Service.exe`
- [ ] `MultirIntegraModulab.Service.exe.config` ← **IMPORTANT**
- [ ] `workflow-schedule.json`
- [ ] Quartz.dll, Newtonsoft.Json.dll, etc.

---

## 🔧 Modificar Configuració en Producció

### ⚠️ NO modificar App.config

Després de compilar, **NO** modifiques `App.config` (al codi font).

### ✅ Modificar el .exe.config

Modifiques el fitxer `.exe.config` **directament al servidor**:

```powershell
# Exemple: Canviar entorn de Preproduccio a Produccio
notepad "C:\Program Files\MultirIntegraModulabService\MultirIntegraModulab.exe.config"

# Buscar:
<add key="Entorn" value="Preproduccio" />

# Canviar a:
<add key="Entorn" value="Produccio" />

# Guardar i tancar
```

**IMPORTANT**: 
- No cal recompilar
- Si és un servei, cal **reiniciar-lo** perquè llegeixi els canvis:
  ```powershell
  Restart-Service MultirIntegraModulabService
  ```

---

## 📝 Exemple Pràctic: Configuració MultirRevisioVigencia

### App.config original (abans de compilar):

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
	<add key="Entorn" value="Preproduccio" />
	<add key="LimitDiagnosticsAProcessar" value="0" />
  </appSettings>
  <connectionStrings>
	<add name="MySqlMultiR_Produccio" 
		 connectionString="Server=zeus;Database=marsa;..." />
	<add name="MySqlMultiR_Preproduccio" 
		 connectionString="Server=zeus;Database=marsa_test;..." />
  </connectionStrings>
</configuration>
```

### Després de compilar (bin\Release\):

Es crea automàticament:
```
MultirRevisioVigencia.exe.config
```

Amb el **mateix contingut** que l'original.

### En execució:

Quan executes `MultirRevisioVigencia.exe`, llegeix:
```csharp
ConfigurationManager.AppSettings["Entorn"]  
// → "Preproduccio"

ConfigurationManager.ConnectionStrings["MySqlMultiR_Preproduccio"]
// → "Server=zeus;Database=marsa_test;..."
```

---

## ✅ CONCLUSIÓ

1. **SÍ**, l'`App.config` es té en compte després de compilar
2. Es **copia automàticament** com a `[Executable].exe.config`
3. **Tots els teus projectes** estan ben configurats
4. En desplegar, **copia els fitxers `.exe.config`** juntament amb els `.exe`
5. Per modificar configuració en producció, **edita el `.exe.config`** directament

---

## 📞 Referències

- **Documentació Microsoft**: [Application Configuration Files](https://docs.microsoft.com/en-us/dotnet/framework/configure-apps/)
- **Desplegament**: Veure `DEPLOYMENT-README.md` i `PRODUCTION-READY-SUMMARY.md`

---

**Data**: 25/01/2026  
**Verificat**: ✅ Tots els projectes tenen App.config correctament configurat
