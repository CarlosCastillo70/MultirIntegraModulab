# ?? Configuració de Signals a Seq

## ?? Objectiu

Els **Signals** a Seq permeten classificar i filtrar logs automàticament segons criteris específics (nivell de log, propietats, etc.). Això facilita trobar **errors**, **warnings** i altres logs importants ràpidament.

---

## ? Per Què Tots els Logs Surten com "Signal: None"?

Quan acabes d'instal·lar Seq o acabes de començar a enviar logs, **Seq no té signals configurats**. Per defecte, tots els logs surten sense classificació (Signal: None).

**Solució:** Has de **crear signals** o **esperar que Seq els detecti automàticament**.

---

## ? Mètode 1: Creació Automàtica de Signals (Recomanat)

Seq pot crear signals automàticament quan detecta logs amb nivells específics.

### **Pas 1: Generar Logs de Prova**

**L'aplicació ja genera logs de prova automàticament!** Cada vegada que executis l'aplicació, es generaran logs amb tots els nivells.

```powershell
# Executar l'aplicació (genera automàticament logs de prova)
.\MultirIntegraModulab.exe
```

Hauràs de veure al log:
```
?? Log de DEBUG - Aquest és un missatge de debugging
?? Log de INFORMATION - Aquest és un missatge informatiu
?? Log de WARNING - Aquest és un avís
? Log de ERROR - Aquest és un error
?? Log de FATAL - Aquest és un error crític
?? Logs de prova generats. Ara pots configurar signals a Seq (http://localhost:5341)
```

**NOTA:** Després de configurar els signals a Seq, pots **comentar** aquestes línies al fitxer `Program.cs`:

```csharp
// ?? TEMPORAL: Generar logs de prova amb diferents nivells per configurar Seq
// NOTA: Comentar aquesta línia després de configurar els signals a Seq
// loggerService.GenerarLogsDeProva();
// loggerService.Info("?? Logs de prova generats...");
```
- ?? **Debug** - Logs de depuració
- ?? **Information** - Logs informatius
- ?? **Warning** - Avisos
- ? **Error** - Errors
- ?? **Fatal** - Errors crítics

### **Pas 2: Verificar a Seq**

1. Obre **http://localhost:5341**
2. Hauries de veure els logs amb diferents nivells
3. A la columna **Level**, veuràs: INF, WRN, ERR, FTL

### **Pas 3: Crear Signals Automàticament**

Seq hauria de detectar automàticament els warnings i errors i proposar-te crear signals.

Si no ho fa automàticament, passa al **Mètode 2**.

---

## ? Mètode 2: Creació Manual de Signals

### **Pas 1: Accedir a Signals**

1. Obre **http://localhost:5341**
2. Fes clic a **Signals** al menú lateral esquerre

### **Pas 2: Crear Signal per Warnings**

1. Fes clic a **New Signal**
2. Omple els camps:

   | Camp | Valor |
   |------|-------|
   | **Title** | `Warnings` |
   | **Description** | `Avisos i alertes del sistema` |
   | **Filter** | `@Level = 'Warning'` |
   | **Color** | ?? Groc/Taronja |

3. Fes clic a **Save**

### **Pas 3: Crear Signal per Errors**

1. Fes clic a **New Signal**
2. Omple els camps:

   | Camp | Valor |
   |------|-------|
   | **Title** | `Errors` |
   | **Description** | `Errors del sistema` |
   | **Filter** | `@Level = 'Error'` |
   | **Color** | ?? Vermell |

3. Fes clic a **Save**

### **Pas 4: Crear Signal per Fatal**

1. Fes clic a **New Signal**
2. Omple els camps:

   | Camp | Valor |
   |------|-------|
   | **Title** | `Fatal Errors` |
   | **Description** | `Errors crítics del sistema` |
   | **Filter** | `@Level = 'Fatal'` |
   | **Color** | ?? Morat/Negre |

3. Fes clic a **Save**

---

## ?? Signals Avançats (Opcional)

Pots crear signals més específics per al teu projecte:

### **Signal per Errors de Base de Dades**

```
@Level = 'Error' and @Exception like '%Oracle%'
```

### **Signal per Avisos de Pacients**

```
@Level = 'Warning' and @Message like '%pacient%'
```

### **Signal per Errors de WebService**

```
@Level = 'Error' and @Message like '%WebService%'
```

### **Signal per Processament de Mostres**

```
Application = 'MultirIntegraModulab' and (@Level = 'Warning' or @Level = 'Error')
```

---

## ?? Propietats Disponibles per Filtres

Pots utilitzar aquestes propietats als filtres:

| Propietat | Descripció | Exemple |
|-----------|------------|---------|
| `@Level` | Nivell de log | `Debug`, `Information`, `Warning`, `Error`, `Fatal` |
| `@Message` | Missatge del log | `@Message like '%error%'` |
| `@Exception` | Tipus d'excepció | `@Exception like '%SqlException%'` |
| `@Timestamp` | Data i hora | `@Timestamp >= DateTime.Today` |
| `Application` | Nom de l'aplicació | `Application = 'MultirIntegraModulab'` |
| `Environment` | Entorn | `Environment = 'Produccio'` |
| `ThreadId` | ID del fil d'execució | `ThreadId = 1` |

---

## ?? Verificar que els Signals Funcionen

### **Pas 1: Executar l'Aplicació**

```powershell
.\MultirIntegraModulab.exe
```

### **Pas 2: Accedir a Seq**

1. Obre **http://localhost:5341**
2. Fes clic a **Signals** al menú lateral
3. Hauries de veure els signals creats amb comptadors:
   - ?? **Warnings** (X events)
   - ?? **Errors** (X events)
   - ?? **Fatal Errors** (X events)

### **Pas 3: Filtrar per Signal**

1. A la pàgina principal de logs, veuràs una columna **Signal**
2. Fes clic sobre un signal (ex: **Warnings**)
3. Es filtraran només els logs d'aquell signal

---

## ?? Exemple Visual

Després de configurar els signals, la teva pantalla de Seq hauria de mostrar:

```
???????????????????????????????????????????????????????????
? SIGNALS                                                 ?
???????????????????????????????????????????????????????????
? ?? Warnings                    12 events in last hour   ?
? ?? Errors                       3 events in last hour   ?
? ?? Fatal Errors                 0 events in last hour   ?
???????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????
? EVENTS                                                  ?
???????????????????????????????????????????????????????????
? [11:30:45] [WRN] ?? Seq no està disponible      ??      ?
? [11:30:40] [INF] ? Seq connectat correctament          ?
? [11:30:35] [ERR] ? Error processant mostra     ??      ?
???????????????????????????????????????????????????????????
```

---

## ?? Alertes Basades en Signals (Pro Feature)

Si tens **Seq Pro**, pots configurar alertes automàtiques:

1. Accedeix a **Settings ? Alerts**
2. Crea una nova alerta:
   - **Trigger:** Signal = "Errors"
   - **Condition:** Count > 5 in 10 minutes
   - **Action:** Send email / Slack / Teams

**Nota:** Aquesta funcionalitat només està disponible a Seq Pro (versió de pagament).

---

## ?? Solució de Problemes

### **Problema: No em sorteix l'opció "Signals"**

**Causa:** Seq Free té signals limitats.

**Solució:** Els signals bàsics estan disponibles a Seq Free. Si no els veus, assegura't que:
1. Tens la última versió de Seq instal·lada
2. Has enviat logs amb diferents nivells (Warning, Error)

### **Problema: Els signals no es creen automàticament**

**Solució:** Crea'ls manualment seguint el **Mètode 2** d'aquesta guia.

### **Problema: Els filtres no funcionen**

**Causa:** Sintaxi incorrecta al filtre.

**Solució:** Utilitza la sintaxi correcta:
- ? Correcte: `@Level = 'Warning'`
- ? Incorrecte: `Level = Warning` (sense @ i sense cometes)

---

## ?? Més Informació

- **Documentació oficial de Signals**: https://docs.datalust.co/docs/signal-expressions
- **Exemples de filtres**: https://docs.datalust.co/docs/the-seq-query-language
- **Best practices**: https://docs.datalust.co/docs/signals-best-practices

---

## ? Checklist Final

- [ ] Seq instal·lat i en funcionament
- [ ] Logs arribant a Seq amb diferents nivells
- [ ] Signal creat per **Warnings** (`@Level = 'Warning'`)
- [ ] Signal creat per **Errors** (`@Level = 'Error'`)
- [ ] Signal creat per **Fatal** (`@Level = 'Fatal'`)
- [ ] Signals visibles al menú lateral
- [ ] Logs classificats correctament per signal

---

**Darrera actualització:** 26/01/2025  
**Versió:** 1.0  
**Autor:** MultirIntegraModulab Team
