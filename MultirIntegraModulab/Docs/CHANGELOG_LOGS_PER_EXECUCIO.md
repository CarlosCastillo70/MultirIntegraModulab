# Canvi del Sistema de Logging - Logs per Execució

## Data: 27 de gener de 2025

## Resum del Canvi

S'ha modificat el sistema de logging perquè cada execució de l'aplicació generi el seu propi fitxer de log independent amb timestamp d'hora.

## Motivació

L'aplicació s'executarà múltiples vegades al dia (aproximadament cada hora) i es necessita:
1. Tenir logs separats per cada execució
2. Enviar per email només el log de l'execució actual, no logs agregats

## Format Anterior vs Nou

### **ABANS:**
```
multir2025-01-27.log  (tots els logs del dia en un sol fitxer)
```

### **DESPRÉS:**
```
multir2025-01-27_08-00-15.log  (execució de les 8:00:15)
multir2025-01-27_09-00-10.log  (execució de les 9:00:10)
multir2025-01-27_10-00-05.log  (execució de les 10:00:05)
...
```

## Exemple de Noms de Fitxer

Format: `multir{AAAA-MM-DD}_{HH-mm-ss}.log`

Exemples reals:
- `multir2025-01-27_08-30-15.log`
- `multir2025-01-27_14-45-32.log`
- `multir2025-01-27_20-15-08.log`

## Funcionament

1. **Inicialització automàtica**: 
   - La primera crida a qualsevol mètode de logging (`Info`, `Error`, etc.) fixa el timestamp de l'execució
   - Aquest timestamp es manté constant durant tota l'execució

2. **Fitxer únic per execució**:
   - Tots els logs d'una execució van al mateix fitxer
   - El fitxer es crea amb el timestamp del primer log

3. **Email amb log específic**:
   - L'email adjunta només el log de l'execució actual
   - No s'envien logs d'execucions anteriors

## Canvis en el Codi

### Fitxers Modificats

1. **`Logger.cs`** (classe estàtica base):
   - Afegides variables estàtiques: `_dataInici` i `_rutaLogActual`
   - Nou mètode privat: `InicialitzarExecucio()`
   - Nous mètodes públics: `ObtenirRutaLogActual()`, `ExisteixLogActual()`, `ObtenirMidaLogActual()`
   - Mètodes anteriors marcats com `[Obsolete]` però mantinguts per compatibilitat
   - Nou mètode: `ReiniciarPerNovaExecucio()` (per testing)

2. **`LoggerService.cs`** (adaptador):
   - Mètodes actualitzats per utilitzar les noves versions `*Actual()`

3. **Compatibilitat Enrere**:
   - Els mètodes antics (`ObtenirRutaLogAvui()`, etc.) encara funcionen
   - Internament criden als nous mètodes
   - Marcats com obsolets amb warnings de compilació

## Avantatges

✅ **Logs separats per execució**: Cada execució té el seu propi fitxer clarament identificat

✅ **Email precís**: L'email només conté el log de l'execució actual

✅ **Troubleshooting millorat**: És molt més fàcil trobar el log d'una execució específica

✅ **Historial complet**: Es mantenen tots els logs de totes les execucions

✅ **Timestamping automàtic**: El timestamp es fixa automàticament al primer log

✅ **Thread-safe**: Gestió segura amb locks per evitar problemes de concurrència

## Manteniment de Logs

- **Neteja automàtica**: El mètode `NetejaarLogsAntics()` continua funcionant
- **Retenció per defecte**: 30 dies (configurable)
- **Patró de cerca**: `multir*.log` (captura tots els formats)

## Exemples d'Ús

### Execució Normal
```csharp
// Al principi del programa
loggerService.MarcarIniciExecucio();  // Crea: multir2025-01-27_08-30-15.log

loggerService.Info("Processant dades...");
loggerService.Warning("Advertència detectada");
loggerService.Error("Error processat");

// Al final del programa
loggerService.MarcarFinalExecucio();
```

### Obtenir Ruta del Log per Email
```csharp
// Obtenir el log de l'execució actual
string logFilePath = loggerService.ObtenirRutaLogAvui();

// Enviar email amb aquest log específic
emailService.EnviarEmailResumProcessament(resum, logFilePath);
```

## Notes Importants

⚠️ **Timestamp fix per execució**: El timestamp es fixa amb la primera crida a logging. No canvia durant l'execució.

⚠️ **Un fitxer per procés**: Si l'aplicació es reinicia, es crea un nou fitxer de log.

⚠️ **Testing**: Utilitzeu `Logger.ReiniciarPerNovaExecucio()` per simular múltiples execucions en tests.

## Testing

Per provar el nou sistema:

1. Executeu l'aplicació diverses vegades
2. Comproveu que es creen fitxers diferents a la carpeta `Logs/`
3. Verificar que l'email adjunta només el log de l'execució actual

## Rollback (si fos necessari)

Si es necessita tornar enrere al sistema anterior (un log per dia):
1. Els mètodes antics encara existeixen
2. Modificar `InicialitzarExecucio()` per usar format sense hora: `yyyy-MM-dd`
3. O recuperar versió anterior des de Git

## Compatibilitat

- ✅ Compatible amb .NET Framework 4.8
- ✅ Compatible amb C# 7.3
- ✅ Backward compatible amb codi existent
- ✅ Els emails continuen funcionant igual
