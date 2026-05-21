# ?? Guia Ràpida: Configurar Signals a Seq en 5 Minuts

## ?? Context

Acabes d'executar l'aplicació i has vist aquests missatges:

```
?? Log de DEBUG - Aquest és un missatge de debugging
?? Log de INFORMATION - Aquest és un missatge informatiu
?? Log de WARNING - Aquest és un avís
? Log de ERROR - Aquest és un error
?? Log de FATAL - Aquest és un error crític
?? Logs de prova generats. Ara pots configurar signals a Seq (http://localhost:5341)
```

Ara segueix aquests passos per configurar els **Signals** a Seq i classificar automàticament els logs.

---

## ? Passos Ràpids (5 minuts)

### **1?? Obre Seq**

Obre el navegador i ves a: **http://localhost:5341**

---

### **2?? Accedeix a Signals**

Al menú lateral esquerre, fes clic a: **"Signals"**

---

### **3?? Crea el Signal per Warnings**

1. Fes clic al botó **"New Signal"** (dalt a la dreta)
2. Omple els camps:
   - **Title:** `Warnings`
   - **Description:** `Avisos i alertes del sistema`
   - **Filter:** `@Level = 'Warning'`
   - **Choose a color:** Selecciona **?? Groc/Taronja**
3. Fes clic a **"Save"**

---

### **4?? Crea el Signal per Errors**

1. Fes clic al botó **"New Signal"**
2. Omple els camps:
   - **Title:** `Errors`
   - **Description:** `Errors del sistema`
   - **Filter:** `@Level = 'Error'`
   - **Choose a color:** Selecciona **?? Vermell**
3. Fes clic a **"Save"**

---

### **5?? Crea el Signal per Fatal (Opcional)**

1. Fes clic al botó **"New Signal"**
2. Omple els camps:
   - **Title:** `Fatal Errors`
   - **Description:** `Errors crítics del sistema`
   - **Filter:** `@Level = 'Fatal'`
   - **Choose a color:** Selecciona **?? Morat/Negre**
3. Fes clic a **"Save"**

---

## ? Verificació

### **Comprova que els Signals estan actius:**

1. Torna a la pàgina principal de Seq (fes clic a **"Events"** al menú)
2. Executa l'aplicació de nou: `.\MultirIntegraModulab.exe`
3. Hauries de veure els logs amb els signals assignats:

```
??????????????????????????????????????????????????????????
? [11:30:45] [WRN] ?? Log de WARNING    ? ?? Warnings   ?
? [11:30:45] [ERR] ? Log de ERROR      ? ?? Errors     ?
? [11:30:45] [FTL] ?? Log de FATAL      ? ?? Fatal      ?
? [11:30:45] [INF] ?? Log de INFO       ? (none)        ?
??????????????????????????????????????????????????????????
```

### **Comprova el menú Signals:**

Al menú **"Signals"**, hauries de veure:

```
?? Warnings        1 event in last hour
?? Errors          1 event in last hour
?? Fatal Errors    1 event in last hour
```

---

## ?? Neteja (Opcional)

Un cop configurat, pots **desactivar** la generació automàtica de logs de prova.

### **Edita `Program.cs`:**

Cerca aquestes línies (al voltant de la línia 30):

```csharp
// ?? TEMPORAL: Generar logs de prova amb diferents nivells per configurar Seq
// NOTA: Comentar aquesta línia després de configurar els signals a Seq
loggerService.GenerarLogsDeProva();
loggerService.Info("?? Logs de prova generats. Ara pots configurar signals a Seq (http://localhost:5341)");
```

**Comenta-les:**

```csharp
// ?? TEMPORAL: Generar logs de prova amb diferents nivells per configurar Seq
// NOTA: Comentar aquesta línia després de configurar els signals a Seq
// loggerService.GenerarLogsDeProva();
// loggerService.Info("?? Logs de prova generats. Ara pots configurar signals a Seq (http://localhost:5341)");
```

**Recompila:**

```powershell
dotnet build --configuration Release
```

**Ara l'aplicació només generarà logs reals!**

---

## ?? Resultat Final

Després de configurar els signals:

? **Els warnings es classifiquen automàticament** com a ?? Warnings  
? **Els errors es classifiquen automàticament** com a ?? Errors  
? **Els fatals es classifiquen automàticament** com a ?? Fatal Errors  
? **Pots filtrar ràpidament** fent clic sobre un signal  
? **Tens visibilitat completa** de l'estat de l'aplicació

---

## ?? Més Informació

Per configuracions avançades, consulta:

- **[SEQ_INTEGRACIO.md](SEQ_INTEGRACIO.md)** - Guia completa d'integració
- **[SEQ_SIGNALS_CONFIG.md](SEQ_SIGNALS_CONFIG.md)** - Configuració avançada de signals
- **[SEQ_DEBUGGING.md](SEQ_DEBUGGING.md)** - Resolució de problemes

---

## ?? Enhorabona!

Ja tens Seq completament configurat amb classificació automàtica de logs! ??

Ara pots:
- ?? Buscar errors ràpidament
- ?? Detectar avisos abans que es converteixin en problemes
- ?? Analitzar el comportament de l'aplicació en temps real
- ?? Crear dashboards personalitzats

---

**Darrera actualització:** 26/01/2025  
**Temps estimat:** 5 minuts  
**Dificultat:** ? Fàcil
