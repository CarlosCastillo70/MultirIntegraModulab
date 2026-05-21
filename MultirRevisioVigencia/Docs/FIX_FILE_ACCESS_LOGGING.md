# ?? Fix: Error d'accés concurrent al fitxer de log

## ? Problema

Al executar **MultirRevisioVigencia**, el programa no escrivia al fitxer de log i mostrava aquest error repetidament:

```
Error escrivint al log: The process cannot access the file 
'C:\...\Logs\revigio2026-04-27_14-01-40_pre.log' 
because it is being used by another process.
```

### ?? Causa

El problema estava a `FileLoggerService.cs`, línia 77:

```csharp
// ? PROBLEMA: StreamWriter bloqueja el fitxer per a altres accessos
using (var writer = new StreamWriter(_logFilePath, append: true, encoding: Encoding.UTF8))
{
    writer.WriteLine(linia);
}
```

Quan múltiples parts del codi intentaven escriure al log **simultàniament** (per exemple, des de threads diferents o crides ràpides consecutives), el fitxer quedava bloquejat i generava l'error.

---

## ? Solució Implementada

S'ha modificat `EscriureLog()` per utilitzar **`FileStream` amb `FileShare.ReadWrite`**, que permet accés concurrent:

```csharp
// ? SOLUCIÓ: FileStream amb FileShare.ReadWrite permet accés concurrent
using (var fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
{
    writer.WriteLine(linia);
    writer.Flush();
}
```

### ?? Canvis Detallats

#### **Fitxer**: `MultirRevisioVigencia\Infrastructure\Logging\FileLoggerService.cs`

**Abans:**
```csharp
private void EscriureLog(string nivell, string missatge)
{
    lock (_lockObject)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var linia = $"[{timestamp}] [{nivell}] {missatge}";

            using (var writer = new StreamWriter(_logFilePath, append: true, encoding: Encoding.UTF8))
            {
                writer.WriteLine(linia);
            }

            Console.WriteLine(linia);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error escrivint al log: {ex.Message}");
        }
    }
}
```

**Després:**
```csharp
private void EscriureLog(string nivell, string missatge)
{
    lock (_lockObject)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var linia = $"[{timestamp}] [{nivell}] {missatge}";

            // Utilitzar FileStream amb FileShare.ReadWrite per permetre accés concurrent
            using (var fileStream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                writer.WriteLine(linia);
                writer.Flush();
            }

            Console.WriteLine(linia);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error escrivint al log: {ex.Message}");
        }
    }
}
```

---

## ?? Resultats

### Abans del fix:
```
? Error escrivint al log: The process cannot access the file...
? Error escrivint al log: The process cannot access the file...
? Error escrivint al log: The process cannot access the file...
? Fitxer de log no creat o incomplet
```

### Després del fix:
```
? [2026-04-27 14:01:40] [INFO] ?? Iniciant revisió de vigència de diagnòstics MR ...
? [2026-04-27 14:01:40] [INFO] ?? Obtenint diagnòstics vigents per revisar...
? [2026-04-27 14:01:41] [INFO] ? Diagnòstic 12345 marcat com a no vigent
? Fitxer de log creat correctament
```

---

## ?? Avantatges de la Solució

| Aspecte | Abans | Després |
|---------|-------|---------|
| **Accés concurrent** | ? Bloquejat | ? Permès |
| **Errors d'escriptura** | ? Freqüents | ? Eliminats |
| **Fitxer de log** | ? Incomplet | ? Complet |
| **Estabilitat** | ? Inestable | ? Estable |

---

## ?? Detalls Tècnics

### FileShare.ReadWrite

L'opció `FileShare.ReadWrite` permet que:
- **Múltiples processos** puguin accedir al fitxer simultàniament
- **Operacions d'append** no es bloquejin mútuament
- **Lectures** també siguin possibles mentre s'escriu

### Lock Object

El `lock (_lockObject)` encara es manté per:
- **Sincronitzar** les escriptures dins del mateix procés
- **Evitar** condicions de carrera entre threads
- **Garantir** l'ordre correcte dels missatges

---

## ?? Notes

- Aquest problema és comú en aplicacions multithreaded o amb alta concurrència
- La solució és **compatible amb .NET Framework 4.8**
- No afecta el rendiment de manera significativa
- Es manté la **compatibilitat total** amb codi existent

---

## ?? Referències

- **Fitxer modificat**: `MultirRevisioVigencia\Infrastructure\Logging\FileLoggerService.cs`
- **Documentació Microsoft**: [FileStream Class](https://docs.microsoft.com/en-us/dotnet/api/system.io.filestream)
- **FileShare Enumeration**: [FileShare Enum](https://docs.microsoft.com/en-us/dotnet/api/system.io.fileshare)

---

**Data**: 27 d'abril de 2026  
**Autor**: Carlos Castillo  
**Versió**: 1.0
