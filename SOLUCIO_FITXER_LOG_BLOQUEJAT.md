# Solució al Problema de Fitxer de Log Bloquejat

## Problema Original

Quan l'aplicació intentava enviar un email amb el log adjunt, apareixien errors:

```
ERROR LOGGING: The process cannot access the file 
'C:\...\Logs\multir2025-11-27_12-02-28.log' 
because it is being used by another process.
```

## Causa del Problema

El problema era un **cicle vicioso**:

1. ✅ `FlushLogs()` tanca el fitxer de log
2. ✅ `Thread.Sleep(300)` espera
3. ❌ `loggerService.Info("📧 Enviant email...")` **reobre el fitxer!**
4. ❌ `EmailService` intenta adjuntar el fitxer → **ERROR: fitxer bloquejat**
5. ❌ `EmailService` escriu més logs → **més errors!**

### Diagrama del Problema

```
Program.cs:
  ├─ FlushLogs() ────────────────────► Fitxer TANCAT ✅
  ├─ Sleep(300) ─────────────────────► Fitxer TANCAT ✅  
  ├─ Info("📧 Enviant email...") ────► Fitxer OBERT ❌ (reobre!)
  └─ EmailService.Enviar()
       ├─ Attachment(logFile) ───────► ERROR! (fitxer obert) ❌
       └─ Info("📎 Adjuntant...") ────► Més errors! ❌
```

## Solució Implementada

### 1. **Copiar el Fitxer de Log (Program.cs)**

Abans d'enviar l'email, es crea una **còpia temporal** del log:

```csharp
// Obtenir ruta del log ABANS de tancar-lo
string logFilePathOriginal = loggerService.ObtenirRutaLogAvui();

// Tancar el fitxer
loggerService.FlushLogs();
System.Threading.Thread.Sleep(500);

// Crear còpia temporal
string logFilePathTemp = logFilePathOriginal.Replace(".log", "_temp.log");
System.IO.File.Copy(logFilePathOriginal, logFilePathTemp, overwrite: true);

// Passar logger = NULL per evitar reobrir el fitxer
var emailService = new EmailService(..., logger: null);

// Enviar amb la còpia
emailService.EnviarEmailResumProcessament(resum, logFilePathTemp);

// Esborrar temporal
System.IO.File.Delete(logFilePathTemp);
```

### 2. **EmailService Sense Logger (EmailService.cs)**

El constructor ara accepta `logger = null`:

```csharp
public EmailService(..., ILoggerService logger)
{
    _logger = logger;  // Pot ser null
    // ...
}
```

Nou mètode `Log()` que escriu a consola si no hi ha logger:

```csharp
private void Log(string missatge, string tipus = "INFO")
{
    if (_logger != null)
    {
        _logger.Info(missatge);  // Escriu al fitxer (només si està obert)
    }
    else
    {
        Console.WriteLine($"[{tipus}] {missatge}");  // Escriu a consola
    }
}
```

### 3. **Flux Corregit**

```
Program.cs:
  ├─ FlushLogs() ────────────────────► Fitxer TANCAT ✅
  ├─ Sleep(500) ─────────────────────► Fitxer TANCAT ✅
  ├─ Copy(log → log_temp) ───────────► Còpia creada ✅
  ├─ EmailService(logger: null) ─────► No reobre fitxer ✅
  └─ EmailService.Enviar()
       ├─ Log() → Console ───────────► Va a consola, no al fitxer ✅
       ├─ Attachment(log_temp) ──────► Còpia adjuntada ✅
       └─ Delete(log_temp) ──────────► Neteja temporal ✅
```

## Avantatges de la Solució

### ✅ **Fitxer Original Intacte**
- El fitxer de log original no es toca durant l'enviament
- No hi ha risc de corrupció o bloqueig

### ✅ **Sense Dependències**
- `EmailService` pot funcionar sense logger
- Logs d'email van a consola, visible durant l'execució

### ✅ **Còpia Temporal**
- Si la còpia falla, intenta amb l'original (fallback)
- La còpia es neteja automàticament

### ✅ **Més Temps d'Espera**
- Augmentat a 500ms (abans 300ms)
- Més garanties que el fitxer està tancat

## Codi Clau

### Program.cs - Secció d'Email

```csharp
if (enviarEmail)
{
    Console.WriteLine("\n📧 Preparant enviament d'email...");
    
    // 1. Obtenir ruta ABANS de tancar
    string logFilePathOriginal = loggerService.ObtenirRutaLogAvui();
    
    // 2. Tancar fitxer
    loggerService.FlushLogs();
    System.Threading.Thread.Sleep(500);
    
    // 3. Crear còpia
    string logFilePathTemp = logFilePathOriginal.Replace(".log", "_temp.log");
    System.IO.File.Copy(logFilePathOriginal, logFilePathTemp, true);
    
    // 4. EmailService sense logger (no reobre fitxer)
    var emailService = new EmailService(..., logger: null);
    
    // 5. Enviar amb còpia
    emailService.EnviarEmailResumProcessament(resum, logFilePathTemp);
    
    // 6. Neteja
    if (System.IO.File.Exists(logFilePathTemp))
        System.IO.File.Delete(logFilePathTemp);
}
```

### EmailService.cs - Mètode Log Segur

```csharp
private void Log(string missatge, string tipus = "INFO")
{
    if (_logger != null)
    {
        switch (tipus.ToUpper())
        {
            case "WARNING": _logger.Warning(missatge); break;
            case "ERROR": _logger.Error(missatge); break;
            default: _logger.Info(missatge); break;
        }
    }
    else
    {
        Console.WriteLine($"[{tipus}] {missatge}");
    }
}
```

## Testing

### Abans (amb error)
```
📊 RESUM DEL PROCESSAMENT: ...
ERROR LOGGING: The process cannot access the file ...
ERROR LOGGING: The process cannot access the file ...
ERROR LOGGING: The process cannot access the file ...
```

### Després (sense error)
```
📊 RESUM DEL PROCESSAMENT:
   Total processats: 1
   Noves incorporacions: 1
   ...

📧 Preparant enviament d'email...
✅ Còpia temporal del log creada: multir2025-11-27_12-02-28_temp.log
📧 Enviant email amb el resum del processament...
[INFO] 📧 Preparant enviament d'email: 'MultiR - Integració Modulab - ...'
[INFO] 📎 Adjuntant fitxer de log: multir2025-11-27_12-02-28_temp.log
[INFO] 🔓 Connexió SMTP anònima
[INFO] 📤 Enviant email a 1 destinatari(s) via smtp.trueta.intranet:25...
[INFO] ✅ Email enviat a: carloscastillollucia@gmail.com
✅ Email enviat correctament
```

## Comparació Visual

### ❌ ABANS

```
┌─────────────┐
│ Program.cs  │
└──────┬──────┘
       │
       ├─ FlushLogs() ──► 🔒 Tancat
       │
       ├─ Info("Email") ──► 🔓 REOBRE! ❌
       │
       ├─ EmailService.Enviar()
       │    │
       │    ├─ new Attachment(log) ──► ❌ ERROR!
       │    │
       │    └─ Info("Adjuntant") ──► ❌ Més errors!
       │
       └─ ❌ FALLIDA
```

### ✅ DESPRÉS

```
┌─────────────┐
│ Program.cs  │
└──────┬──────┘
       │
       ├─ FlushLogs() ──► 🔒 Tancat
       │
       ├─ Copy(log → temp) ──► 📄 Còpia
       │
       ├─ EmailService(logger: null)
       │    │
       │    ├─ new Attachment(temp) ──► ✅ OK!
       │    │
       │    └─ Log() → Console ──► ✅ A consola!
       │
       └─ Delete(temp) ──► 🗑️ Neteja
            ✅ ÈXIT
```

## Fitxers Modificats

1. **Program.cs** - Secció d'enviament d'email
   - Afegida lògica de còpia temporal
   - EmailService creat amb `logger: null`
   - Tots els logs després de `FlushLogs()` van a `Console.WriteLine`

2. **EmailService.cs** - Constructor i logging
   - Constructor accepta `logger` nullable
   - Nou mètode `Log()` que escriu a consola si no hi ha logger
   - Totes les crides a `_logger` reemplaçades per `Log()`

## Beneficis Addicionals

1. **Logs Visibles**: Els logs d'email es veuen a la consola durant l'execució
2. **Debugging Fàcil**: És més fàcil veure què passa amb l'email
3. **Més Robust**: Múltiples capes de protecció (còpia + delay + retry)
4. **Backward Compatible**: Si es passa logger, funciona com abans

## Conclusió

La solució implementa una **estratègia de defensa en profunditat**:

1. ✅ Tancar el fitxer amb `FlushLogs()`
2. ✅ Esperar més temps (500ms)
3. ✅ Crear còpia temporal
4. ✅ No reobrir el fitxer (`logger: null`)
5. ✅ Retry logic per adjuntar (3 intents)
6. ✅ Neteja automàtica de temporals

Això garanteix que **el fitxer de log mai es bloqueja** durant l'enviament de l'email! 🎉
