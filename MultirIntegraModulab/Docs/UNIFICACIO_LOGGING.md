# ?? Unificació del Sistema de Logging

## ?? Problema Identificat

El projecte **MultirIntegraModulab** tenia **dos sistemes de logging independents** funcionant simultàniament:

### 1?? Sistema Modern: **Serilog** (via `LoggerService`)
- **Format**: `[2026-04-27 13:53:10.765] [INF] Missatge...`
- **Fitxer**: `Logs\multir2026-04-27_13-53-10_pre.log`
- **Utilització**: Classes modernes que utilitzen `ILoggerService`

### 2?? Sistema Legacy: **Logger estàtic**
- **Format**: `2026-04-27 13:53:11 INFO : Missatge...`
- **Fitxer**: `Logs\multir2026-04-27_13-53-11_pre.log` (fitxer diferent!)
- **Utilització**: Classes legacy que criden directament `Logger.Info()`, `Logger.Error()`, etc.

### ? Conseqüències

- **Dos fitxers de log diferents** creats durant la mateixa execució
- **Logs fragmentats**: missatges repartits entre dos fitxers
- **Confusió** a l'hora d'analitzar logs d'errors
- **Inconsistència** en formats de timestamp i nivells de log

---

## ? Solució Implementada

S'ha **modificat la classe `Logger` estàtica** perquè **redirigeixi totes les crides cap a Serilog**, unificant així tots els logs en un únic fitxer amb format consistent.

### ?? Canvis Realitzats

#### **Fitxer**: `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\Logger.cs`

**1. Afegit referència a Serilog:**
```csharp
using Serilog;
```

**2. Modificat el mètode `EscriureMissatge`:**

**? Abans** (escrivia a fitxer propi):
```csharp
private static void EscriureMissatge(TipusLog tipus, string missatge)
{
    // Escrivia directament a un fitxer amb StreamWriter
    using (var writer = new StreamWriter(_rutaLogActual, append: true))
    {
        writer.WriteLine(lineaLog);
        writer.Flush();
    }
}
```

**? Després** (redirigeix a Serilog):
```csharp
private static void EscriureMissatge(TipusLog tipus, string missatge)
{
    switch (tipus)
    {
        case TipusLog.INFO:
            Log.Information(missatge);
            break;
        case TipusLog.ERROR:
            Log.Error(missatge);
            break;
        case TipusLog.WARNING:
            Log.Warning(missatge);
            break;
        case TipusLog.DEBUG:
            Log.Debug(missatge);
            break;
        case TipusLog.TRACE:
            Log.Verbose(missatge);
            break;
    }
}
```

**3. Actualitzat `ObtenirRutaLogActual()`:**

Ara intenta obtenir la ruta des de la configuració de Serilog (`App.config`) abans de retornar la ruta legacy:

```csharp
public static string ObtenirRutaLogActual()
{
    var rutaConfig = ConfigurationManager.AppSettings["RutaFitxerLog"];
    if (!string.IsNullOrEmpty(rutaConfig))
    {
        var entorn = ConfigurationManager.AppSettings["Entorn"] ?? "Preproduccio";
        var suffixEntorn = entorn.Equals("Produccio", StringComparison.OrdinalIgnoreCase) ? "pro" : "pre";
        return string.Format(rutaConfig, DateTime.Now, suffixEntorn);
    }
    
    return _rutaLogActual; // Fallback
}
```

---

## ?? Resultat

### ? **Abans dels canvis**:

Dos fitxers de log creats durant una execució:

```
?? Logs/
  ?? multir2026-04-27_13-53-10_pre.log  (Serilog)
     [2026-04-27 13:53:10.765] [INF] ?? Començem a processar les mostres ...
     [2026-04-27 13:53:10.892] [INF] ?? Total mostres: 25
     
  ?? multir2026-04-27_13-53-11_pre.log  (Logger estàtic)
     2026-04-27 13:53:11 INFO : ?? Inserint auditoria amb codi 'NMRCMP'
     2026-04-27 13:53:12 INFO : ? Diagnòstic marcat com a no vigent
```

### ? **Després dels canvis**:

**Un únic fitxer de log** amb tots els missatges:

```
?? Logs/
  ?? multir2026-04-27_13-53-10_pre.log  (Serilog unificat)
     [2026-04-27 13:53:10.765] [INF] ?? Començem a processar les mostres ...
     [2026-04-27 13:53:10.892] [INF] ?? Total mostres: 25
     [2026-04-27 13:53:11.134] [INF] ?? Inserint auditoria amb codi 'NMRCMP'
     [2026-04-27 13:53:12.023] [INF] ? Diagnòstic marcat com a no vigent
```

---

## ?? Avantatges

| Aspecte | Abans | Després |
|---------|-------|---------|
| **Fitxers de log** | 2 fitxers per execució | ? 1 fitxer únic |
| **Format** | Inconsistent (2 formats) | ? Format unificat (Serilog) |
| **Cerca d'errors** | Buscar en 2 fitxers | ? Buscar en 1 fitxer |
| **Timestamps** | Diferent precisió | ? Mil·lisegons consistents |
| **Anàlisi de logs** | Difícil (logs fragmentats) | ? Fàcil (cronologia unificada) |

---

## ?? Compatibilitat

### ? **Codi legacy continua funcionant**

Totes les crides existents a `Logger.Info()`, `Logger.Error()`, etc. **continuen funcionant sense modificacions**:

```csharp
// Aquest codi NO cal modificar-lo!
Logger.Info("?? Inserint auditoria amb codi 'NMRCMP'");
Logger.Error("Error processant mostra", exception);
Logger.Warning("Pacient sense èxitus registrat");
```

### ?? **Migració gradual recomanada**

Tot i que el sistema legacy continua funcionant, es recomana migrar progressivament a `ILoggerService`:

**? Codi legacy:**
```csharp
public class MevaClasse
{
    public void ProcessarDades()
    {
        Logger.Info("Processant dades...");
    }
}
```

**? Codi modern:**
```csharp
public class MevaClasse
{
    private readonly ILoggerService _logger;
    
    public MevaClasse(ILoggerService logger)
    {
        _logger = logger;
    }
    
    public void ProcessarDades()
    {
        _logger.Info("Processant dades...");
    }
}
```

---

## ?? Notes Finals

- La classe `Logger` estàtica ara actua com un **wrapper** sobre Serilog
- **No cal modificar cap codi existent** per beneficiar-se dels canvis
- Es manté la **compatibilitat total** amb codi legacy
- Els logs ara estan **unificats i ordenats cronològicament**
- Facilita l'**anàlisi i debugging** amb una única font de veritat

---

## ?? Referències

- **Fitxer modificat**: `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\Logger.cs`
- **Sistema de logging modern**: `MultirIntegraModulab\Infrastructure\ExternalServices\Logger\LoggerService.cs`
- **Documentació Serilog**: [SERILOG_INTEGRATION.md](SERILOG_INTEGRATION.md)

---

**Data d'implementació**: 27 d'abril de 2026  
**Autor**: Carlos Castillo  
**Versió**: 1.0
