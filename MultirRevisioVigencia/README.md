# MultirRevisioVigencia

Aplicació per revisar automàticament la vigència dels diagnòstics de pacients del sistema MultiR.

## 📋 Descripció

Aquest aplicatiu s'executa diàriament per revisar els diagnòstics marcats com a vigents (`vigent = 'S'`) i comprovar si compleixen algun dels **3 criteris de desactivació automàtica**.

## 🎯 Funcionalitat

### Comprovacions Automàtiques

L'aplicació revisa cada diagnòstic vigent i comprova:

1. **🔴 Pacient Èxitus** (Motiu: `E`)
   - Si el pacient ha mort → Desactivar diagnòstic

2. **🔴 Vigència Superada** (Motiu: `V`) - *Només Multiresistents*
   - Si han passat més dies dels configurats des de l'últim positiu → Desactivar diagnòstic
   - No aplica a Virus Respiratoris

3. **🔴 Mostres Negatives Consecutives** (Motiu: `N`) - *Només Multiresistents* ⭐ **NOU**
   - Si hi ha prou mostres negatives consecutives per tots els tipus de mostra → Desactivar diagnòstic
   - Sistema de 3 fonts: Regles + Mostres Positives + Acumulació
   - Positius posteriors reinicien el comptador
   - No aplica a Virus Respiratoris

### Procés d'Execució

1. **Obté diagnòstics vigents**: Cerca tots els diagnòstics amb `vigent = 'S'`
2. **Comprova criteris**: Per cada diagnòstic, avalua els 3 criteris
3. **Marca no vigents**: Els que compleixen algun criteri es marquen amb:
   - `vigent = 'N'`
   - `data_no_vigent = NOW()`
   - `responsable_no_vigent = 'MULTIR_AUTOM'`
   - `motiu_no_vigent = 'E' | 'V' | 'N'`
4. **Genera logs**: Registra tots els resultats i errors als fitxers de log

## 🏗️ Arquitectura

```
MultirRevisioVigencia/
├── Application/
│   ├── UseCases/
│   │   └── RevisarVigenciaDiagnosticsUseCase.cs
│   ├── Services/
│   │   └── MostresNegativesService.cs           ⭐ NOU
│   └── DTOs/
│       ├── ResumRevisioVigenciaDto.cs
│       ├── DiagnosticPerRevisar.cs
│       ├── ReglaTipusMostra.cs                  ⭐ NOU
│       ├── MostraPositivaDiagnostic.cs          ⭐ NOU
│       └── MostraDiagnostic.cs                  ⭐ NOU
├── Domain/
│   └── Interfaces/
│       ├── IMultiRRepository.cs
│       └── ILoggerService.cs
├── Infrastructure/
│   ├── Configuration/
│   │   └── ConfiguracioManager.cs
│   ├── Persistence/
│   │   └── LegacyServices/
│   │       ├── MultiRDbService.cs
│   │       └── MultiRDbService.MostresNegatives.cs  ⭐ NOU
│   └── Logging/
│       └── SerilogLoggerService.cs
├── Docs/
│   └── SISTEMA_MOSTRES_NEGATIVES.md             ⭐ NOU
├── Program.cs
├── App.config
└── MultirRevisioVigencia.csproj
```

## ⚙️ Configuració

### App.config

```xml
<appSettings>
  <!-- IMPORTANT: Canviar a "Produccio" abans de desplegar -->
  <add key="Entorn" value="Preproduccio" />
  
  <!-- Logging -->
  <add key="RutaFitxerLog" value="Logs\revigio{0:yyyy-MM-dd_HH-mm-ss}_{1}.log" />
</appSettings>

<connectionStrings>
  <!-- PRODUCCIÓ -->
  <add name="MySqlMultiR_Produccio"
       connectionString="Server=zeus;Database=marsa;Uid=marsa;Pwd=2a0d9a8d22;"
       providerName="MySql.Data.MySqlClient" />

  <!-- PREPRODUCCIÓ -->
  <add name="MySqlMultiR_Preproduccio"
       connectionString="Server=zeus;Database=marsa_test;Uid=marsa;Pwd=2a0d9a8d22;"
       providerName="MySql.Data.MySqlClient" />
</connectionStrings>
```

### Selecció d'Entorn

L'aplicació selecciona automàticament la connexió a la base de dades segons el valor de `Entorn`:

- **`Entorn = "Preproduccio"`** → Utilitza `MySqlMultiR_Preproduccio` (Database: **marsa_test**)
- **`Entorn = "Produccio"`** → Utilitza `MySqlMultiR_Produccio` (Database: **marsa**)


⚠️ **IMPORTANT**: Abans de desplegar a producció, canviar `<add key="Entorn" value="Produccio" />`

## 📚 Documentació Addicional

### Problemes Resolts

- **[Fix: Error d'accés concurrent al fitxer de log](Docs/FIX_FILE_ACCESS_LOGGING.md)** - Solució al problema "The process cannot access the file because it is being used by another process"
- **[Migració a Serilog](Docs/MIGRACIO_SERILOG.md)** - Unificació del sistema de logging amb Serilog per consistència amb MultirIntegraModulab
- **[Comparació Implementació Serilog](Docs/COMPARACIO_IMPLEMENTACIO_SERILOG.md)** - Diferències entre la implementació de MultirIntegraModulab i MultirRevisioVigencia
- **[Eliminació Emails Automàtics](Docs/ELIMINACIO_EMAILS_AUTOMATICS.md)** - Eliminació de l'enviament automàtic d'emails
- **[Eliminació Codi Obsolet](Docs/ELIMINACIO_CODI_OBSOLET.md)** - Neteja de codi obsolet després de la migració a Serilog

### Guies

- Configuració d'entorns (Producció vs Preproducció)
- Programació de tasques amb Windows Task Scheduler
- Monitorització de logs i errors

## 🔧 Requisits

- .NET Framework 4.8
- MySQL 5.7+
- Accés a la base de dades `marsa` (producció) o `marsa_test` (preproducció)
- **Serilog 4.3.1+** (per logging estructurat)

## 🚀 Execució

### Manual

```bash
MultirRevisioVigencia.exe
```

### Programada (Task Scheduler)

1. Obrir **Programador de tasques** de Windows
2. Crear nova tasca bàsica
3. Configurar:
   - **Desencadenador**: Diari a les 02:00 AM
   - **Acció**: Iniciar programa → `MultirRevisioVigencia.exe`
   - **Condicions**: Executar només si l'ordinador està connectat a la xarxa

## 📊 Logs

Els logs es guarden a: `Logs\revigio{data}_{entorn}.log`

Exemple:
- **Preproducció**: `Logs\revigio2026-04-27_14-01-40_pre.log`
- **Producció**: `Logs\revigio2026-04-27_14-01-40_pro.log`

Format de log (Serilog):
```
[2026-04-27 14:01:40.765] [INF] 🔍 Iniciant revisió de vigència de diagnòstics MR ...
[2026-04-27 14:01:40.892] [INF] 📋 Obtenint diagnòstics vigents per revisar...
[2026-04-27 14:01:41.123] [INF]    Trobats 150 diagnòstic(s) vigent(s) per revisar
```

> **Nota**: Utilitzem **Serilog** per logging estructurat amb mil·lisegons per millor precisió i consistència amb MultirIntegraModulab.

## 📊 Resum de l'Execució

L'aplicació mostra un resum complet per **consola** i **fitxer de log**:

- Total diagnòstics revisats
- Diagnòstics marcats com a no vigents
  - Per èxitus del pacient
  - Per superar vigència
- Diagnòstics amb error
- Durada de l'execució

> **Nota**: L'aplicació **NO envia emails automàtics**. Els resultats es registren als fitxers de log per revisió manual.

## 🛠️ Manteniment

### Neteja de Logs Antics

Es recomana implementar una tasca de neteja de logs antics:

```bash
# Eliminar logs de més de 30 dies
forfiles /p "C:\path\to\Logs" /s /m *.log /d -30 /c "cmd /c del @path"
```

---

**Versió**: 1.0  
**Data**: Abril 2026  
**Autor**: Carlos Castillo

