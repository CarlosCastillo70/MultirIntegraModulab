# ?? IMPORTANT: Logging Unificat

## ?? Resum Executiu

S'ha **unificat el sistema de logging** del projecte **MultirIntegraModulab** per evitar la creació de múltiples fitxers de log durant una mateixa execució.

### ? Abans
- **2 fitxers de log** per execució
- Logs fragmentats entre dos fitxers amb formats diferents

### ? Després
- **1 únic fitxer de log** amb format unificat (Serilog)
- Tots els logs ordenats cronològicament

---

## ?? Canvis Tècnics

La classe `Logger` estàtica (legacy) ara **redirigeix automàticament** totes les crides cap a **Serilog**.

### Codi Legacy Continua Funcionant ?

```csharp
// Aquest codi NO cal modificar-lo - funciona automàticament amb Serilog
Logger.Info("Processant dades...");
Logger.Error("Error trobat", exception);
Logger.Warning("Atenció: situació inesperada");
```

### Millor Pràctica ??

Per a **nou codi**, utilitzar `ILoggerService` amb injecció de dependències:

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

## ?? Localització dels Logs

Els logs es troben a:
```
MultirIntegraModulab\Logs\multir{data}_{entorn}.log
```

Exemple:
- **Preproducció**: `Logs\multir2026-04-27_14-30-15_pre.log`
- **Producció**: `Logs\multir2026-04-27_14-30-15_pro.log`

---

## ?? Documentació Completa

Per més detalls, consultar:
- [UNIFICACIO_LOGGING.md](UNIFICACIO_LOGGING.md) - Documentació completa dels canvis
- [SERILOG_INTEGRATION.md](SERILOG_INTEGRATION.md) - Configuració de Serilog

---

**Data**: 27/04/2026 | **Versió**: 1.0
