# 📋 Tractament de Mostres Antigues

## 🎯 Objectiu

Implementar el tractament de mostres antigues que es troben registrades a l'historial però sense les dates `data_resultat` i `data_validacio`.

## 📖 Descripció

Durant uns dies, es poden trobar mostres a l'historial (amb el mateix número d'etiqueta) que no tenen incorporat ni data de mostra, ni data de validació.

Aquest és un tema **temporal** que desapareixerà passats uns dies, quan totes les mostres tinguin les dates actualitzades.

## 🔍 Detecció de Mostres Antigues

Una mostra es considera **antiga** quan:

```
data_resultat = NULL AND data_validat = NULL
```

Això indica que la mostra es va incorporar abans del canvi que va implementar el registre de dates.

## ⚙️ Tractament Implementat

### 1️⃣ Actualització de Dates

Quan es detecta una mostra antiga, s'actualitzen les dates amb els valors d'Oracle:

```sql
UPDATE pacients_diagnostics_mostra  
SET data_resultat = DATA_RESULTAT,  
    data_validacio = DATA_VALIDACIO,  -- pot ser NULL
    estat_integracio_m = CASE  
        WHEN DATA_VALIDACIO IS NOT NULL THEN 'V'  
        ELSE 'P' 
    END,
    dt_update = NOW()
WHERE etiqueta = 'ETIQUETA_ID' 
  AND dt_delete IS NULL
```

### 2️⃣ Inserció d'Auditoria

S'insereix un registre d'auditoria amb el codi **EMCA** (Estat Mostra Cas Antic):

```csharp
bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
    mostra,
    "EMCA",
    primerResultat,
    null);
```

### 3️⃣ Fi del Processament

**No es continua endavant** amb el tractament de la mostra. Es passa a la següent mostra.

```csharp
return false; // No continuar processament
```

## 📊 Flux de Processament

```
┌─────────────────────────────────────┐
│ Mostra detectada com "Antiga"       │
│ (data_resultat=NULL, data_validat=NULL)│
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 1. Obtenir dates d'Oracle           │
│    - Data Resultat                  │
│    - Data Validació (pot ser NULL)  │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 2. Actualitzar dates a MySQL        │
│    UPDATE pacients_diagnostics_mostra│
│    SET data_resultat = ...          │
│        data_validacio = ...         │
│        estat_integracio_m = ...     │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 3. Inserir auditoria EMCA           │
│    (Estat Mostra Cas Antic)         │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ 4. Passar a la següent mostra       │
│    (NO continuar processament)      │
└─────────────────────────────────────┘
```

## 💻 Implementació

### Mètode Principal

```csharp
/// <summary>
/// Tracta una mostra antiga: actualitza les dates
/// </summary>
private bool TractarMostraAntigua(Mostra mostra, ResumProcessamentDto resum)
{
    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}⚠️ Mostra antiga (sense dates) - actualitzant dates...");

    try
    {
        // Obtenir les dates del primer resultat
        var primerResultat = mostra.Resultats[0];
        var dataResultat = primerResultat.DataResultat;
        var dataValidacio = primerResultat.DataValidacio;

        // Actualitzar les dates a la base de dades
        bool actualitzat = _multiRRepository.ActualitzarResultatAntic(
            mostra.EtiquetaId,
            dataResultat,
            dataValidacio);

        if (actualitzat)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✅ Dates actualitzades correctament");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   - Data resultat: {dataResultat:dd/MM/yyyy HH:mm}");
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}   - Data validació: {(dataValidacio.HasValue ? dataValidacio.Value.ToString("dd/MM/yyyy HH:mm") : "NULL")}");
        }

        // Inserir auditoria amb codi EMCA
        bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
            mostra,
            "EMCA",
            primerResultat,
            null);
    }
    catch (Exception ex)
    {
        _logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}❌ Error tractant mostra antiga: {ex.Message}", ex);
        resum.MostresAmbError++;
    }

    return false; // No continuar processament
}
```

### Mètode d'Actualització

El mètode `ActualitzarResultatAntic` ja està implementat a `MultiRDbServiceExtensions.cs`:

```csharp
public bool ActualitzarResultatAntic(string etiquetaId, DateTime dataResultat, DateTime? dataValidacio)
{
    try
    {
        using (var conn = new MySqlConnection(_connectionString))
        {
            conn.Open();
            
            string sql = @"UPDATE pacients_diagnostics_mostra 
                          SET data_resultat = @dataResultat,
                              data_validacio = @dataValidacio,
                              estat_integracio_m = CASE 
                                  WHEN @dataValidacio IS NOT NULL THEN 'V'
                                  ELSE 'P' 
                              END,
                              dt_update = NOW()
                          WHERE etiqueta = @etiqueta 
                          AND dt_delete IS NULL";
            
            // ... execució SQL ...
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"Error actualitzant resultat antic {etiquetaId}: {ex.Message}", ex);
        return false;
    }
}
```

## 📝 Codi d'Auditoria EMCA

### Taula: integracio_modulab_resultats

```sql
INSERT INTO integracio_modulab_resultats (codi, descripcio)
VALUES ('EMCA', 'Estat Mostra Cas Antic - Mostra sense dates a MySQL, actualitzades amb dates d''Oracle');
```

### Significat

- **Codi**: `EMCA`
- **Descripció**: Estat Mostra Cas Antic
- **Quan s'utilitza**: Quan una mostra no té dates a MySQL i s'actualitzen amb les dates d'Oracle

## 📊 Estadístiques

Les mostres antigues es comptabilitzen al resum final:

```csharp
resum.MostresAntigues++; // Comptador al ResumProcessamentDto
```

## 🔧 Configuració de Base de Dades

### Abans d'executar, cal:

1. **Executar l'script SQL** per afegir el codi EMCA:
   ```bash
   mysql -u user -p database < MultirIntegraModulab/Docs/SQL_INSERT_AUDIT_CODES.sql
   ```

2. **Verificar** que el codi s'ha inserit correctament:
   ```sql
   SELECT * FROM integracio_modulab_resultats WHERE codi = 'EMCA';
   ```

## 📈 Logs Esperats

Quan es processa una mostra antiga, es veuen aquests logs:

```
⚠️ Mostra antiga (sense dates) - actualitzant dates...
   ✅ Dates actualitzades correctament
      - Data resultat: 15/01/2024 10:30
      - Data validació: 15/01/2024 14:25
   ✅ Auditoria EMCA (Estat Mostra Cas Antic) creada correctament
```

## ⚠️ Consideracions

1. **Temporal**: Aquest tractament és temporal. Passats uns dies, no hi haurà més mostres antigues.

2. **No continuar processament**: És important **no continuar** amb el processament normal després d'actualitzar les dates.

3. **Dates del primer resultat**: S'utilitzen les dates del primer resultat, ja que tots els resultats de la mateixa etiqueta tenen les mateixes dates.

4. **Validació NULL**: La data de validació pot ser NULL si la mostra no està validada.

## 🔄 Relació amb Altres Tipus d'Incorporació

| Tipus | Descripció | Continuar processament? |
|-------|------------|-------------------------|
| Nova | Mostra nova sense historial | ✅ Sí |
| Antiga | Mostra sense dates | ❌ No |
| Repetida | Dates idèntiques | ❌ No |
| Validada | Nova data validació | ✅ Sí |
| Revalidada | Data validació diferent | ✅ Sí |
| Desvalidada | Sense validació a Oracle | ❌ No |

## 📚 Referències

- **Enum**: `TipusIncorporacio.Antiga`
- **Use Case**: `ProcessarMostresUseCase.TractarMostraAntigua()`
- **Repository**: `IMultiRRepository.ActualitzarResultatAntic()`
- **Service**: `MultiRDbServiceExtensions.ActualitzarResultatAntic()`
- **Audit Code**: `EMCA` a `integracio_modulab_resultats`
