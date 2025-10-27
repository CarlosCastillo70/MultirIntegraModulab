# 📋 Tractament de Mostres Desvalidades - Implementació Mejorada

## 🎯 Objectiu

Implementar un tractament intel·ligent de mostres desvalidades que compara la mostra actual amb la mostra entrant per determinar si hi ha canvis i actuar en conseqüència.

## 📖 Descripció

Una mostra es considera **desvalidada** quan:

- **Oracle**: `DATA_VALIDACIO = NULL`
- **MySQL**: `data_validacio <> NULL`

Això significa que la mostra tenia una data de validació històricament però ara es capta sense data de validació.

## 🔀 Dos Casos Possibles

### 🔵 Cas 1: Mostres Idèntiques (sense canvis)

Si la mostra actual i la nova són **idèntiques**:

1. **Actualitzar** només la `data_validacio` a `NULL`
2. **Actualitzar** `estat_integracio_m` a `'P'` (Pendent)
3. **Inserir auditoria** amb codi **EMCD** (Estat Mostra Cas Desvalidat sense canvis)
4. **NO continuar** amb el processament (passar a la següent mostra)

#### SQL Executat:

```sql
UPDATE pacients_diagnostics_mostra  
SET data_validacio = NULL, 
    estat_integracio_m = 'P',
    dt_update = NOW()
WHERE etiqueta = 'ETIQUETA_ID' 
AND dt_delete IS NULL
```

### 🔴 Cas 2: Mostres Diferents (amb canvis)

Si la mostra actual i la nova són **diferents**:

1. **Guardar historial** amb els canvis detectats
2. **Esborrar dades** actuals de la mostra (soft delete)
3. **Continuar endavant** amb el tractament normal de la mostra (reprocessar amb noves dades)

## 🔍 Comparació de Mostres

El sistema compara els següents camps:

- **Data resultat**
- **Data validació**
- **Tipus de mostra** (MOSTRA_DESCRIPCIO)
- **Tipus de prova** (PROVA_DESCRIPCIO)
- **Microorganismes** associats

## 📊 Flux de Processament

```
┌─────────────────────────────────────────┐
│ Mostra detectada com "Desvalidada"      │
│ (Oracle: data_validacio=NULL            │
│  MySQL:  data_validacio<>NULL)          │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│ 1. Obtenir mostra existent de MySQL    │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│ 2. Comparar mostres                     │
│    (CompararMostres)                    │
└─────────────┬───────────────────────────┘
              │
              ▼
        ┌─────────────┐
        │ Hi ha canvis?│
        └──┬───────┬───┘
           │       │
        NO │       │ SÍ
           │       │
           ▼       ▼
    ┌──────────┐  ┌──────────────────────┐
    │ CAS 1    │  │ CAS 2                │
    │ Sense    │  │ Amb canvis           │
    │ canvis   │  │                      │
    └────┬─────┘  └─────┬────────────────┘
         │              │
         ▼              ▼
┌─────────────────┐  ┌──────────────────────┐
│ Actualitzar     │  │ Guardar historial    │
│ data_validacio  │  │ amb detall dels      │
│ a NULL          │  │ canvis               │
└────┬────────────┘  └─────┬────────────────┘
     │                     │
     ▼                     ▼
┌─────────────────┐  ┌──────────────────────┐
│ Inserir         │  │ Esborrar dades       │
│ auditoria EMCD  │  │ (soft delete)        │
└────┬────────────┘  └─────┬────────────────┘
     │                     │
     ▼                     ▼
┌─────────────────┐  ┌──────────────────────┐
│ Retornar FALSE  │  │ Retornar TRUE        │
│ (no continuar)  │  │ (continuar           │
│                 │  │  processament)       │
└─────────────────┘  └──────────────────────┘
```

## 💻 Implementació

### Mètode Principal

```csharp
/// <summary>
/// Tracta una mostra desvalidada: compara amb mostra existent i decideix acció
/// Si són idèntiques: actualitza data_validacio a NULL i estat a 'P', insereix auditoria EMCD
/// Si són diferents: guarda historial, esborra dades i continua processament
/// </summary>
private bool TractarMostraDesvalidada(Mostra mostra, ResumProcessamentDto resum)
{
    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🗑️ Mostra desvalidada - comprovant canvis...");

    try
    {
        // 1. Obtenir la mostra existent de la base de dades
        var mostraExistent = _multiRRepository.ObtenirMostraDiagnostic(mostra.EtiquetaId);
        
        if (mostraExistent == null)
        {
            _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ No s'ha trobat mostra existent per comparar");
            resum.MostresAmbError++;
            return false;
        }

        // 2. Comparar mostres per detectar canvis
        var resultatComparacio = _multiRRepository.CompararMostres(mostraExistent, mostra);

        if (!resultatComparacio.HiHaCanvis)
        {
            // CAS 1: No hi ha canvis - només actualitzar data_validacio a NULL
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ Mostres idèntiques - actualitzant data_validacio a NULL...");

            bool actualitzat = _multiRRepository.ActualitzarDataValidacio(mostra.EtiquetaId, null);

            if (actualitzat)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Data validació actualitzada a NULL i estat_integracio_m a 'P'");
            }
            else
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut actualitzar la data de validació");
            }

            // Inserir auditoria EMCD (Estat Mostra Cas Desvalidat sense canvis)
            var primerResultat = mostra.Resultats[0];
            bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
                mostra,
                "EMCD",
                primerResultat,
                null);

            if (auditoriaCreada)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✅ Auditoria EMCD (Estat Mostra Cas Desvalidat sense canvis) creada correctament");
            }
            else
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut crear l'auditoria EMCD");
            }

            return false; // No continuar processament
        }
        else
        {
            // CAS 2: Hi ha canvis - guardar historial, esborrar i continuar
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔄 Mostres diferents - guardant historial i esborrant dades...");
            
            // Mostrar canvis detectats
            foreach (var canvi in resultatComparacio.CanvisDetectats)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   📝 {canvi}");
            }

            // Guardar historial abans d'esborrar
            var tipusCanvi = "DESVALIDADA";
            var observacions = $"Mostra desvalidada amb canvis - {string.Join(", ", resultatComparacio.CanvisDetectats)}";
            
            bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
                mostra.EtiquetaId,
                tipusCanvi,
                observacions);

            if (historialGuardat)
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Historial guardat correctament");
            }
            else
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠️ No s'ha pogut guardar l'historial");
            }

            // Esborrar dades de la mostra
            bool esborrat = _multiRRepository.EsborrarDadesMostra(mostra.EtiquetaId);
            
            if (!esborrat)
            {
                _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}❌ Error esborrant mostra desvalidada");
                resum.MostresAmbError++;
                return false;
            }
            else
            {
                _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Dades esborrades correctament");
            }

            // Continuar processament per re-processar la mostra amb les noves dades
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}➡️ Continuant processament amb noves dades...");
            return true; // Continuar processament
        }
    }
    catch (Exception ex)
    {
        _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error tractant mostra desvalidada: {ex.Message}", ex);
        resum.MostresAmbError++;
        return false;
    }
}
```

### Mètodes de Suport

#### ObtenirMostraDiagnostic

```csharp
public MostraDiagnosticExistent ObtenirMostraDiagnostic(string etiquetaId)
{
    // Obté les dades completes de la mostra existent a MySQL
    // Retorna: MostraDiagnosticExistent o null si no existeix
}
```

#### CompararMostres

```csharp
public ResultatComparacioMostres CompararMostres(
    MostraDiagnosticExistent mostraExistent, 
    Mostra mostraEntrant)
{
    // Compara tots els camps rellevants entre les dues mostres
    // Retorna: ResultatComparacioMostres amb HiHaCanvis i CanvisDetectats
}
```

#### ActualitzarDataValidacio

```csharp
public bool ActualitzarDataValidacio(string etiquetaId, DateTime? dataValidacio)
{
    // Actualitza data_validacio i estat_integracio_m
    // Si dataValidacio = NULL → estat_integracio_m = 'P'
    // Si dataValidacio <> NULL → estat_integracio_m = 'V'
}
```

## 📝 Codi d'Auditoria EMCD

### Taula: integracio_modulab_resultats

```sql
INSERT INTO integracio_modulab_resultats (codi, descripcio)
VALUES ('EMCD', 'Estat Mostra Cas Desvalidat sense canvis - Mostra desvalidada idèntica, només actualitzada data_validacio a NULL');
```

### Significat

- **Codi**: `EMCD`
- **Descripció**: Estat Mostra Cas Desvalidat sense canvis
- **Quan s'utilitza**: Quan una mostra desvalidada és idèntica a l'existent i només s'actualitza `data_validacio` a NULL

## 📈 Logs Esperats

### Cas 1: Sense Canvis

```
🗑️ Mostra desvalidada - comprovant canvis...
   ✅ Mostres idèntiques - actualitzant data_validacio a NULL...
      ✔️ Data validació actualitzada a NULL i estat_integracio_m a 'P'
      ✅ Auditoria EMCD (Estat Mostra Cas Desvalidat sense canvis) creada correctament
```

### Cas 2: Amb Canvis

```
🗑️ Mostra desvalidada - comprovant canvis...
   🔄 Mostres diferents - guardant historial i esborrant dades...
      📝 Data resultat: 15/01/2024 10:30 -> 15/01/2024 11:00
      📝 Data validació: 16/01/2024 14:25 -> NULL
      ✔️ Historial guardat correctament
      ✔️ Dades esborrades correctament
      ➡️ Continuant processament amb noves dades...
```

## 🔧 Configuració de Base de Dades

### Abans d'executar, cal:

1. **Executar l'script SQL** per afegir el codi EMCD:
   ```bash
   mysql -u user -p marsa < MultirIntegraModulab/Docs/SQL_INSERT_AUDIT_CODE_EMCD.sql
   ```

2. **Verificar** que el codi s'ha inserit correctament:
   ```sql
   SELECT * FROM integracio_modulab_resultats WHERE codi = 'EMCD';
   ```

## 📊 Estadístiques

Les mostres desvalidades es comptabilitzen al resum final:

```csharp
resum.MostresDesvalidades++; // Comptador al ResumProcessamentDto
```

## 🔄 Relació amb Altres Tipus d'Incorporació

| Tipus | Descripció | Comparar mostres? | Continuar processament? |
|-------|------------|-------------------|-------------------------|
| Nova | Mostra nova sense historial | ❌ No | ✅ Sí |
| Antiga | Mostra sense dates | ❌ No | ❌ No |
| Repetida | Dates idèntiques | ❌ No | ❌ No |
| Validada | Nova data validació | ❌ No | ✅ Sí |
| Revalidada | Data validació diferent | ❌ No | ✅ Sí |
| Desvalidada (sense canvis) | Oracle sense validació, MySQL amb validació, dades idèntiques | ✅ Sí | ❌ No |
| Desvalidada (amb canvis) | Oracle sense validació, MySQL amb validació, dades diferents | ✅ Sí | ✅ Sí |

## ⚠️ Consideracions

1. **Comparació intel·ligent**: El sistema compara tots els camps rellevants per determinar si hi ha canvis reals.

2. **Historial detallat**: Quan hi ha canvis, es guarda un registre detallat amb tots els canvis detectats.

3. **Soft delete**: Les dades no s'esborren físicament, sinó que es marca el camp `dt_delete`.

4. **Reprocessament**: Si hi ha canvis, la mostra es reprocessa completament amb les noves dades d'Oracle.

5. **Auditoria completa**: Tots els casos es registren a la taula d'auditoria per traçabilitat.

## 📚 Referències

- **Enum**: `TipusIncorporacio.Desvalidada`
- **Use Case**: `ProcessarMostresUseCase.TractarMostraDesvalidada()`
- **Repository**: `IMultiRRepository.ObtenirMostraDiagnostic()`, `CompararMostres()`, `ActualitzarDataValidacio()`, `EsborrarDadesMostra()`
- **Service**: `MultiRDbServiceExtensions.ObtenirMostraDiagnostic()`, `CompararMostres()`, `ActualitzarDataValidacio()`, `EsborrarDadesMostra()`
- **Entities**: `MostraDiagnosticExistent`, `ResultatComparacioMostres`
- **Audit Code**: `EMCD` a `integracio_modulab_resultats`

## 🔗 Fitxers Relacionats

- `ProcessarMostresUseCase.cs` - Mètode principal `TractarMostraDesvalidada()`
- `MultiRDbServiceExtensions.cs` - Implementació dels mètodes de comparació i actualització
- `IMultiRRepository.cs` - Interfície amb signatures dels mètodes
- `MostraDiagnosticExistent.cs` - Entitat per representar mostra existent
- `ResultatComparacioMostres.cs` - Entitat per resultat de comparació
- `SQL_INSERT_AUDIT_CODE_EMCD.sql` - Script SQL per afegir codi auditoria
